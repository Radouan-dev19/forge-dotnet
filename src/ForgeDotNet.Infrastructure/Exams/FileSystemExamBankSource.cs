using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.Practice;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Infrastructure.Exams;

public sealed class FileSystemExamBankSource(
    IPracticeExerciseSource exerciseSource,
    ExamBankOptions options) : IExamBankSource, IExamSqlItemSource, IDisposable
{
    private const int MaximumManifestBytes = 128 * 1024;
    private const int MaximumContentFileBytes = 256 * 1024;

    /// <summary>
    /// Bornes des listes d'éligibilité d'une banque d'examen.
    /// </summary>
    /// <remarks>
    /// Ces deux listes énumèrent le vivier dans lequel un tirage puise ; elles grandissent donc avec
    /// le catalogue, alors que <c>drawCount</c> reste petit. Une borne commune à seize les figeait à
    /// la taille du catalogue de l'incrément 09 : passé ce seuil, tout exercice neuf devenait
    /// intirable, et l'audit relevait déjà quarante-trois exercices dans ce cas. La borne des
    /// contraintes d'un item, elle, reste serrée : une consigne compte quelques lignes et n'a aucune
    /// raison de croître.
    ///
    /// La borne réelle sur le volume lu reste <see cref="MaximumManifestBytes"/> ; celles-ci
    /// protègent contre une liste absurde, pas contre un fichier volumineux.
    /// </remarks>
    private const int MaximumEligibleItems = 256;
    private const int MaximumItemConstraints = 16;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private BankSnapshot? _cache;
    private static readonly IReadOnlyList<string> EfExamConstraints = Array.AsReadOnly(new[]
    {
        "Conservez la classe publique Submission et la signature Run.",
        "Utilisez réellement EF Core ; les cas privés varient les entrées.",
        "Aucun accès réseau, fichier externe ou secret n’est autorisé.",
    });

    public async ValueTask<IReadOnlyList<ExamBlueprint>> ListAsync(
        CancellationToken cancellationToken = default) =>
        (await GetSnapshotAsync(cancellationToken)).Blueprints;

    public async ValueTask<ExamBlueprint?> GetAsync(
        string examId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(examId) || examId.Length > 100)
        {
            return null;
        }

        BankSnapshot snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.BlueprintsById.GetValueOrDefault(examId);
    }

    async ValueTask<SqlExamItemDefinition?> IExamSqlItemSource.GetAsync(
        string itemId,
        int itemVersion,
        string contentRevision,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId)
            || itemId.Length > 100
            || itemVersion < 1
            || string.IsNullOrWhiteSpace(contentRevision)
            || contentRevision.Length != 64)
        {
            return null;
        }

        BankSnapshot snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.SqlItems.GetValueOrDefault(new SqlItemKey(itemId, itemVersion, contentRevision));
    }

    private async ValueTask<BankSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        BankSnapshot? current = Volatile.Read(ref _cache);
        if (current is not null)
        {
            return current;
        }

        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            current = Volatile.Read(ref _cache);
            if (current is not null)
            {
                return current;
            }

            BankSnapshot replacement = await LoadSnapshotAsync(cancellationToken);
            Volatile.Write(ref _cache, replacement);
            return replacement;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task<BankSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        string[] manifests = ListManifestPaths();
        var blueprints = new List<ExamBlueprint>(manifests.Length);
        var sqlItems = new Dictionary<SqlItemKey, SqlExamItemDefinition>();
        foreach (string manifest in manifests)
        {
            LoadedExam loaded = await LoadAsync(manifest, cancellationToken);
            blueprints.Add(loaded.Blueprint);
            foreach (SqlExamItemDefinition item in loaded.SqlItems)
            {
                var key = new SqlItemKey(item.ItemId, item.ItemVersion, item.ContentRevision);
                if (!sqlItems.TryAdd(key, item))
                {
                    throw new InvalidDataException("Un item SQL d’examen est défini plusieurs fois.");
                }
            }
        }

        if (blueprints.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != blueprints.Count)
        {
            throw new InvalidDataException("La banque d’examens contient un identifiant dupliqué.");
        }

        ExamBlueprint[] ordered = blueprints.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
        return new BankSnapshot(
            Array.AsReadOnly(ordered),
            ordered.ToDictionary(item => item.Id, StringComparer.Ordinal),
            sqlItems);
    }

    private async ValueTask<LoadedExam> LoadAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await ReadFileAsync(manifestPath, MaximumManifestBytes, cancellationToken);
        using JsonDocument document = ParseJson(bytes, "Le manifeste d’examen est invalide.");
        JsonElement root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetInt32() != 1)
        {
            throw new InvalidDataException("La version du manifeste d’examen n’est pas prise en charge.");
        }

        string[] exerciseIds = ReadOptionalStringArray(root, "eligibleExerciseIds", MaximumEligibleItems);
        string[] efScenarioIds = ReadOptionalStringArray(root, "eligibleEfScenarioIds", MaximumEligibleItems);
        JsonElement.ArrayEnumerator sqlItems = root.TryGetProperty("sqlItems", out JsonElement sqlItemsElement)
            ? sqlItemsElement.EnumerateArray()
            : default;
        var candidates = new List<ExamCandidate>(16);
        var privateSqlItems = new List<SqlExamItemDefinition>(8);

        foreach (string exerciseId in exerciseIds)
        {
            PracticeExercise exercise = await exerciseSource.GetAsync(exerciseId, cancellationToken)
                ?? throw new InvalidDataException("Un exercice compatible de l’examen n’est pas publié.");
            candidates.Add(new ExamCandidate(
                exercise.Id,
                exercise.Version,
                exercise.Revision,
                // Le domaine vient de la compétence travaillée : un item d'examen « api-* »
                // alimente le score Api, sans quoi ce domaine ne pourrait jamais atteindre son seuil.
                MasterySkillDomains.FromSkills(exercise.Skills),
                exercise.Title,
                exercise.Statement,
                exercise.Constraints,
                "Submission.cs",
                exercise.Starter));
        }

        foreach (JsonElement sqlItem in sqlItems)
        {
            (ExamCandidate candidate, SqlExamItemDefinition definition) = ReadSqlItem(bytes, sqlItem);
            candidates.Add(candidate);
            privateSqlItems.Add(definition);
        }

        foreach (string scenarioId in efScenarioIds)
        {
            candidates.Add(await LoadEfCandidateAsync(scenarioId, cancellationToken));
        }

        // Le vivier résolu suit la même borne que les listes d'éligibilité qui le composent : c'est le
        // même ensemble, une fois les identifiants remplacés par les candidats correspondants. Le
        // plancher de deux, lui, reste : un vivier d'un seul item rendrait tout tirage prévisible.
        if (candidates.Count is < 2 or > MaximumEligibleItems
            || candidates.Select(item => item.ItemId).Distinct(StringComparer.Ordinal).Count() != candidates.Count)
        {
            throw new InvalidDataException("La liste compatible de l’examen est invalide.");
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(bytes);
        foreach (ExamCandidate candidate in candidates.OrderBy(item => item.ItemId, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(
                $"{candidate.ItemId}|{candidate.ItemVersion}|{candidate.ContentRevision}|{candidate.SubmissionKind}"));
            hash.AppendData([0]);
        }

        var blueprint = new ExamBlueprint(
            ReadRequiredString(root, "id"),
            root.GetProperty("version").GetInt32(),
            Convert.ToHexString(hash.GetHashAndReset()),
            ReadRequiredString(root, "title"),
            root.GetProperty("durationMinutes").GetInt32(),
            root.GetProperty("drawCount").GetInt32(),
            root.GetProperty("passingScore").GetDecimal(),
            Array.AsReadOnly(candidates.ToArray()));
        ExamRules.ValidateBlueprint(blueprint);
        return new LoadedExam(blueprint, Array.AsReadOnly(privateSqlItems.ToArray()));
    }

    private static (ExamCandidate Candidate, SqlExamItemDefinition Definition) ReadSqlItem(
        byte[] manifestBytes,
        JsonElement item)
    {
        string id = ReadRequiredString(item, "id");
        int version = item.GetProperty("version").GetInt32();
        string revision = ComputeRevision(manifestBytes, Encoding.UTF8.GetBytes(id));
        JsonElement expected = item.GetProperty("expectedResult");
        string[] columns = expected.GetProperty("columns").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
        IReadOnlyList<SqlLabCell>[] rows = expected.GetProperty("rows").EnumerateArray()
            .Select(row => (IReadOnlyList<SqlLabCell>)Array.AsReadOnly(row.EnumerateArray()
                .Select(ReadCell)
                .ToArray()))
            .ToArray();
        var expectation = new SqlLabExpectedResult(
            Array.AsReadOnly(columns),
            Array.AsReadOnly(rows),
            expected.GetProperty("ordered").GetBoolean(),
            expected.GetProperty("numericTolerance").GetDecimal());
        string[] constraints = ReadOptionalStringArray(item, "constraints", MaximumItemConstraints);
        string starter = ReadRequiredString(item, "starterQuery");
        string solution = ReadRequiredString(item, "solutionQuery");
        if (columns.Length is < 1 or > 50
            || columns.Any(string.IsNullOrWhiteSpace)
            || columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Length
            || rows.Length is < 1 or > 100
            || rows.Any(row => row.Count != columns.Length)
            || expectation.NumericTolerance is < 0 or > 1
            || starter.Length > 16_384
            || solution.Length > 16_384)
        {
            throw new InvalidDataException("Le contrat privé d’un item SQL d’examen est invalide.");
        }

        var candidate = new ExamCandidate(
            id,
            version,
            revision,
            MasteryDomain.Sql,
            ReadRequiredString(item, "title"),
            ReadRequiredString(item, "statement"),
            Array.AsReadOnly(constraints),
            "Submission.sql",
            starter,
            ExamSubmissionKind.Sql);
        return (candidate, new SqlExamItemDefinition(id, version, revision, expectation, solution));
    }

    private async ValueTask<ExamCandidate> LoadEfCandidateAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string scenarioRoot = Path.GetFullPath(Path.Combine(contentRoot, "sql", scenarioId));
        EnsureDescendant(contentRoot, scenarioRoot, "Un scénario EF d’examen sort de content/sql.");
        EnsureNoReparsePoints(contentRoot, scenarioRoot);

        string manifestPath = Path.Combine(scenarioRoot, "scenario.json");
        string contractPath = Path.Combine(scenarioRoot, "tests", "contract.json");
        string statementPath = Path.Combine(scenarioRoot, "statement.md");
        string starterPath = Path.Combine(scenarioRoot, "exam", "starter", "Submission.cs");
        string solutionPath = Path.Combine(scenarioRoot, "exam", "solution", "Submission.cs");
        string runnerPath = Path.Combine(scenarioRoot, "exam", "tests", "runner.json");
        string visiblePath = Path.Combine(scenarioRoot, "exam", "tests", "visible", "cases.json");
        string hiddenPath = Path.Combine(scenarioRoot, "exam", "tests", "hidden", "cases.json");
        byte[][] revisionFiles = await ReadFilesAsync(
            [manifestPath, contractPath, statementPath, starterPath, solutionPath, runnerPath, visiblePath, hiddenPath],
            cancellationToken);
        using JsonDocument manifest = ParseJson(revisionFiles[0], "Le manifeste EF d’examen est invalide.");
        using JsonDocument contract = ParseJson(revisionFiles[1], "Le contrat EF d’examen est invalide.");
        JsonElement root = manifest.RootElement;
        if (!string.Equals(ReadRequiredString(root, "id"), scenarioId, StringComparison.Ordinal)
            || !string.Equals(ReadRequiredString(contract.RootElement, "mode"), "ef", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Le scénario EF n’est pas compatible avec un examen de code.");
        }

        string revision = ComputeRevision(revisionFiles);
        return new ExamCandidate(
            scenarioId,
            root.GetProperty("version").GetInt32(),
            revision,
            MasteryDomain.Sql,
            ReadRequiredString(root, "title"),
            StrictUtf8.GetString(revisionFiles[2]),
            EfExamConstraints,
            "Submission.cs",
            StrictUtf8.GetString(revisionFiles[3]),
            ExamSubmissionKind.CSharp);
    }

    private string[] ListManifestPaths()
    {
        string directory = ResolveDirectory();
        string[] manifests = Directory.EnumerateFiles(directory, "exam.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Take(17)
            .ToArray();
        if (manifests.Length is 0 or > 16)
        {
            throw new InvalidDataException("Le volume de la banque d’examens de référence est invalide.");
        }

        return manifests;
    }

    private string ResolveDirectory()
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.BankDirectoryPath));
        EnsureDescendant(root, directory, "La banque d’examens doit rester sous content/.");
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("La banque d’examens de référence est introuvable.");
        }

        EnsureNoReparsePoints(root, directory);
        return directory;
    }

    private async ValueTask<byte[]> ReadFileAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string fullPath = Path.GetFullPath(path);
        EnsureDescendant(contentRoot, fullPath, "Un fichier d’examen sort de content/.");
        EnsureNoReparsePoints(contentRoot, fullPath);
        FileInfo info = new(fullPath);
        if (!info.Exists || info.Length <= 0 || info.Length > maximumBytes)
        {
            throw new InvalidDataException("Un fichier d’examen est absent ou trop volumineux.");
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    private async ValueTask<byte[][]> ReadFilesAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        var values = new byte[paths.Count][];
        for (int index = 0; index < paths.Count; index++)
        {
            values[index] = await ReadFileAsync(paths[index], MaximumContentFileBytes, cancellationToken);
        }

        return values;
    }

    private static JsonDocument ParseJson(byte[] bytes, string message)
    {
        try
        {
            return JsonDocument.Parse(StrictUtf8.GetString(bytes), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 24,
            });
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            throw new InvalidDataException(message, exception);
        }
    }

    private static string[] ReadOptionalStringArray(JsonElement element, string name, int maximumCount)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return [];
        }

        string[] values = value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        if (values.Length > maximumCount
            || values.Any(string.IsNullOrWhiteSpace)
            || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
        {
            throw new InvalidDataException("Une liste du manifeste d’examen est invalide.");
        }

        return values;
    }

    private static SqlLabCell ReadCell(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => new SqlLabCell(null, IsNull: true),
        JsonValueKind.String => new SqlLabCell(value.GetString()),
        JsonValueKind.Number => new SqlLabCell(value.GetDecimal().ToString(CultureInfo.InvariantCulture)),
        JsonValueKind.True => new SqlLabCell("true"),
        JsonValueKind.False => new SqlLabCell("false"),
        _ => throw new InvalidDataException("Une cellule attendue d’examen SQL est invalide."),
    };

    private static string ComputeRevision(params byte[][] parts)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (byte[] part in parts)
        {
            hash.AppendData(part);
            hash.AppendData([0]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string ReadRequiredString(JsonElement element, string name)
    {
        string? value = element.GetProperty(name).GetString();
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException("Un texte obligatoire du manifeste d’examen est absent.")
            : value;
    }

    private static void EnsureDescendant(string parent, string child, string message)
    {
        string normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        string normalizedChild = Path.GetFullPath(child);
        if (!normalizedChild.StartsWith(
            normalizedParent + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidDataException(message);
        }
    }

    private static void EnsureNoReparsePoints(string contentRoot, string targetPath)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRoot));
        string relative = Path.GetRelativePath(root, Path.GetFullPath(targetPath));
        string current = root;
        foreach (string segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Les points de réanalyse sont interdits dans la banque d’examens.");
            }
        }
    }

    public void Dispose() => _loadGate.Dispose();

    private sealed record LoadedExam(
        ExamBlueprint Blueprint,
        IReadOnlyList<SqlExamItemDefinition> SqlItems);

    private sealed record BankSnapshot(
        IReadOnlyList<ExamBlueprint> Blueprints,
        IReadOnlyDictionary<string, ExamBlueprint> BlueprintsById,
        IReadOnlyDictionary<SqlItemKey, SqlExamItemDefinition> SqlItems);

    private readonly record struct SqlItemKey(string ItemId, int ItemVersion, string ContentRevision);
}
