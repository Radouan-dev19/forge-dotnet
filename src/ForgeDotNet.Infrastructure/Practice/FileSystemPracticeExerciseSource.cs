using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Practice;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Infrastructure.Practice;

public sealed class FileSystemPracticeExerciseSource(
    ContentCatalogProvider catalogProvider,
    PracticeContentOptions options) : IPracticeExerciseSource, IDisposable
{
    private const int MaximumPrivateFileBytes = 131_072;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private CacheSnapshot? _cache;

    public async ValueTask<IReadOnlyList<PracticeExercise>> ListAsync(
        CancellationToken cancellationToken = default) =>
        (await GetSnapshotAsync(cancellationToken)).Exercises;

    public async ValueTask<PracticeExercise?> GetAsync(
        string exerciseId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exerciseId))
        {
            return null;
        }

        CacheSnapshot snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.ById.GetValueOrDefault(exerciseId);
    }

    private async ValueTask<CacheSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        string revision = catalogProvider.Current.Revision;
        CacheSnapshot? current = Volatile.Read(ref _cache);
        if (current is not null && string.Equals(current.Revision, revision, StringComparison.Ordinal))
        {
            return current;
        }

        await _reloadGate.WaitAsync(cancellationToken);
        try
        {
            revision = catalogProvider.Current.Revision;
            current = Volatile.Read(ref _cache);
            if (current is not null && string.Equals(current.Revision, revision, StringComparison.Ordinal))
            {
                return current;
            }

            CacheSnapshot replacement = await LoadSnapshotAsync(revision, cancellationToken);
            Volatile.Write(ref _cache, replacement);
            return replacement;
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private async Task<CacheSnapshot> LoadSnapshotAsync(
        string revision,
        CancellationToken cancellationToken)
    {
        var exercises = new List<PracticeExercise>();
        foreach (ContentCatalogItem item in catalogProvider.Current.GetByType(ContentDocumentType.Exercise))
        {
            PracticeExercise? exercise = await LoadAsync(item.Id, cancellationToken);
            if (exercise is not null)
            {
                exercises.Add(exercise);
            }
        }

        PracticeExercise[] values = exercises.ToArray();
        return new CacheSnapshot(
            revision,
            Array.AsReadOnly(values),
            values.ToDictionary(exercise => exercise.Id, StringComparer.Ordinal));
    }

    private async ValueTask<PracticeExercise?> LoadAsync(
        string exerciseId,
        CancellationToken cancellationToken)
    {
        ContentCatalogItem? catalogItem = catalogProvider.Current.FindById(exerciseId);
        if (catalogItem is null || catalogItem.Type != ContentDocumentType.Exercise)
        {
            return null;
        }

        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string catalogDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.CatalogDirectoryPath));
        EnsureDescendant(contentRoot, catalogDirectory, "Le catalogue de pratique doit rester sous content/.");
        string exerciseDirectory = Path.GetFullPath(Path.Combine(catalogDirectory, "exercises", exerciseId));
        EnsureDescendant(catalogDirectory, exerciseDirectory, "Le dossier d'exercice est hors du catalogue publié.");
        EnsureNoReparsePoints(contentRoot, exerciseDirectory);

        LoadedFile manifestFile = await ReadFileAsync(
            contentRoot,
            exerciseDirectory,
            "exercise.json",
            cancellationToken);
        using JsonDocument manifest = ParseJson(manifestFile.Text);
        JsonElement root = manifest.RootElement;
        string id = ReadString(root, "id");
        int version = root.GetProperty("version").GetInt32();
        if (!string.Equals(id, catalogItem.Id, StringComparison.Ordinal) || version != catalogItem.Version)
        {
            throw new InvalidDataException("Le manifeste privé ne correspond pas au catalogue public.");
        }

        LoadedFile statement = await ReadFileAsync(
            contentRoot,
            exerciseDirectory,
            ReadString(root, "statement"),
            cancellationToken);
        LoadedFile explanation = await ReadFileAsync(
            contentRoot,
            exerciseDirectory,
            ReadString(root, "explanation"),
            cancellationToken);
        JsonElement solutionElement = root.GetProperty("solution");
        string solutionDirectory = ReadString(solutionElement, "path");
        IReadOnlyList<LoadedFile> starterFiles = await ReadDirectoryAsync(
            contentRoot, exerciseDirectory, ReadString(root, "starterPath"), cancellationToken);
        IReadOnlyList<LoadedFile> solutionFiles = await ReadDirectoryAsync(
            contentRoot, exerciseDirectory, solutionDirectory, cancellationToken);
        IReadOnlyList<LoadedFile> visibleTestFiles = await ReadDirectoryAsync(
            contentRoot, exerciseDirectory, ReadString(root, "visibleTestsPath"), cancellationToken);
        IReadOnlyList<LoadedFile> hiddenTestFiles = await ReadDirectoryAsync(
            contentRoot, exerciseDirectory, ReadString(root, "hiddenTestsPath"), cancellationToken);
        LoadedFile starter = PreferredSource(starterFiles);
        LoadedFile solution = PreferredSource(solutionFiles);
        LoadedFile? runnerContract = await ReadOptionalFileAsync(
            contentRoot, exerciseDirectory, "tests/runner.json", cancellationToken);
        LoadedFile? reviewCards = await ReadOptionalFileAsync(
            contentRoot, exerciseDirectory, "review-cards.md", cancellationToken);
        string variantId = ReadString(root, "variantId");
        LoadedFile variantManifestFile = await ReadFileAsync(
            contentRoot,
            Path.Combine(catalogDirectory, "exercises", variantId),
            "exercise.json",
            cancellationToken);
        using JsonDocument variantManifest = ParseJson(variantManifestFile.Text);
        JsonElement variantRoot = variantManifest.RootElement;
        if (!string.Equals(ReadString(variantRoot, "id"), variantId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("La variante privée ne correspond pas à son identifiant public.");
        }

        LoadedFile variantStatement = await ReadFileAsync(
            contentRoot,
            Path.Combine(catalogDirectory, "exercises", variantId),
            ReadString(variantRoot, "statement"),
            cancellationToken);
        PracticeHint[] hints = root.GetProperty("hints").EnumerateArray()
            .Select(element => new PracticeHint(
                element.GetProperty("level").GetInt32(),
                ReadString(element, "kind"),
                ReadString(element, "content")))
            .ToArray();
        PracticeExerciseExample[] examples = root.GetProperty("examples").EnumerateArray()
            .Select(element => new PracticeExerciseExample(
                ReadExampleText(element, "input"),
                ReadExampleText(element, "output")))
            .ToArray();
        var revisionFiles = new List<LoadedFile> {
            manifestFile,
            statement,
            explanation,
            variantManifestFile,
            variantStatement,
        };
        revisionFiles.AddRange(starterFiles);
        revisionFiles.AddRange(solutionFiles);
        revisionFiles.AddRange(visibleTestFiles);
        revisionFiles.AddRange(hiddenTestFiles);
        if (runnerContract is not null) revisionFiles.Add(runnerContract);
        if (reviewCards is not null) revisionFiles.Add(reviewCards);
        string revision = ComputeRevision(revisionFiles);
        JsonElement unlock = solutionElement.GetProperty("unlock");
        var exercise = new PracticeExercise(
            id,
            version,
            revision,
            ReadString(root, "title"),
            root.GetProperty("difficulty").GetInt32(),
            root.GetProperty("estimatedMinutes").GetInt32(),
            statement.Text,
            Array.AsReadOnly(root.GetProperty("constraints").EnumerateArray().Select(item => item.GetString()!).ToArray()),
            Array.AsReadOnly(examples),
            starter.Text,
            Array.AsReadOnly(hints),
            unlock.GetProperty("seriousAttempts").GetInt32(),
            TimeSpan.FromMinutes(unlock.GetProperty("minimumDelayMinutes").GetInt32()),
            solution.Text,
            explanation.Text,
            variantId,
            ReadString(variantRoot, "title"),
            variantStatement.Text);
        PracticeRules.ValidateExercise(exercise);
        return exercise;
    }

    private static LoadedFile PreferredSource(IReadOnlyList<LoadedFile> files) =>
        files.FirstOrDefault(file => file.RelativePath.EndsWith("/Submission.cs", StringComparison.OrdinalIgnoreCase))
        ?? files.FirstOrDefault(file => file.RelativePath.EndsWith("/README.md", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidDataException("Un dossier de starter ou solution ne contient aucun fichier utilisable.");

    private static async Task<IReadOnlyList<LoadedFile>> ReadDirectoryAsync(
        string contentRoot,
        string exerciseDirectory,
        string relativeDirectory,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetFullPath(relativeDirectory, exerciseDirectory);
        EnsureDescendant(exerciseDirectory, directory, "Un dossier privé d'exercice sort de son exercice.");
        EnsureNoReparsePoints(contentRoot, directory);
        if (!Directory.Exists(directory))
        {
            throw new InvalidDataException("Un dossier privé obligatoire de l'exercice est introuvable.");
        }

        string[] paths = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Take(33)
            .ToArray();
        if (paths.Length is 0 or > 32)
        {
            throw new InvalidDataException("Le volume d'un dossier privé d'exercice est invalide.");
        }

        var files = new List<LoadedFile>(paths.Length);
        foreach (string path in paths)
        {
            files.Add(await ReadFileAsync(
                contentRoot,
                exerciseDirectory,
                Path.GetRelativePath(exerciseDirectory, path),
                cancellationToken));
        }

        return Array.AsReadOnly(files.ToArray());
    }

    private static async Task<LoadedFile?> ReadOptionalFileAsync(
        string contentRoot,
        string exerciseDirectory,
        string relativePath,
        CancellationToken cancellationToken)
    {
        string path = Path.GetFullPath(relativePath, exerciseDirectory);
        return File.Exists(path)
            ? await ReadFileAsync(contentRoot, exerciseDirectory, relativePath, cancellationToken)
            : null;
    }

    private static async Task<LoadedFile> ReadFileAsync(
        string contentRoot,
        string baseDirectory,
        string relativePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathFullyQualified(relativePath)
            || relativePath.Split('/', '\\').Any(segment => segment is "." or ".."))
        {
            throw new InvalidDataException("Un chemin privé d'exercice est invalide.");
        }

        string fullBase = Path.GetFullPath(baseDirectory);
        EnsureDescendant(contentRoot, fullBase, "Le dossier privé d'exercice est hors de content/.");
        string path = Path.GetFullPath(relativePath, fullBase);
        EnsureDescendant(fullBase, path, "Un fichier privé d'exercice sort de son dossier.");
        EnsureNoReparsePoints(contentRoot, path);
        if (!File.Exists(path))
        {
            throw new InvalidDataException("Un fichier privé obligatoire de l'exercice est introuvable.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        if (bytes.Length == 0 || bytes.Length > MaximumPrivateFileBytes)
        {
            throw new InvalidDataException("La taille d'un fichier privé d'exercice est invalide.");
        }

        try
        {
            return new LoadedFile(
                Path.GetRelativePath(contentRoot, path).Replace('\\', '/'),
                StrictUtf8.GetString(bytes),
                bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Les fichiers privés d'exercice doivent être en UTF-8 strict.", exception);
        }
    }

    private static JsonDocument ParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32,
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Un manifeste privé d'exercice est devenu illisible.", exception);
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        string? value = element.GetProperty(propertyName).GetString();
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException("Un texte privé obligatoire de l'exercice est absent.")
            : value;
    }

    private static string ReadExampleText(JsonElement element, string propertyName)
    {
        JsonElement property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.String
            ? property.GetString()!
            : throw new InvalidDataException("Un exemple privé de l'exercice n'est pas un texte.");
    }

    private static string ComputeRevision(IEnumerable<LoadedFile> files)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (LoadedFile file in files.OrderBy(item => item.RelativePath, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(file.RelativePath));
            hash.AppendData([0]);
            hash.AppendData(file.Bytes);
            hash.AppendData([0]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void EnsureDescendant(string parent, string child, string message)
    {
        string normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        string normalizedChild = Path.GetFullPath(child);
        string prefix = normalizedParent + Path.DirectorySeparatorChar;
        if (!normalizedChild.StartsWith(prefix, PathComparison))
        {
            throw new InvalidDataException(message);
        }
    }

    private static void EnsureNoReparsePoints(string contentRoot, string targetPath)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRoot));
        string target = Path.GetFullPath(targetPath);
        EnsureDescendant(root, target, "Le chemin privé doit rester sous content/.");
        string relative = Path.GetRelativePath(root, target);
        string current = root;
        foreach (string segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Les points de réanalyse sont interdits dans le contenu privé.");
            }
        }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public void Dispose() => _reloadGate.Dispose();

    private sealed record CacheSnapshot(
        string Revision,
        IReadOnlyList<PracticeExercise> Exercises,
        IReadOnlyDictionary<string, PracticeExercise> ById);

    private sealed record LoadedFile(string RelativePath, string Text, byte[] Bytes);
}
