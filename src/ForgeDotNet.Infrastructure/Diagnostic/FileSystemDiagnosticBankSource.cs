using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Infrastructure.Diagnostic;

public sealed class FileSystemDiagnosticBankSource :
    IDiagnosticBankSource,
    IDiagnosticRubricSource,
    IDisposable
{
    private const int MaximumFileBytes = 512 * 1024;
    private const int ExpectedQuestionCount = 36;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly string _answerKeyPath;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly string _questionsPath;
    private readonly string _rubricPath;
    private DiagnosticBank? _bank;
    private DiagnosticScoringRubric? _rubric;

    public FileSystemDiagnosticBankSource(DiagnosticBankOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string bankDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.BankDirectoryPath));
        EnsureContained(bankDirectory, contentRoot);
        _questionsPath = ResolveFile(bankDirectory, options.QuestionsFileName, contentRoot);
        _answerKeyPath = ResolveFile(bankDirectory, options.AnswerKeyFileName, contentRoot);
        _rubricPath = ResolveFile(bankDirectory, options.RubricFileName, contentRoot);
    }

    public async ValueTask<DiagnosticBank> GetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _bank!;
    }

    public async ValueTask<DiagnosticScoringRubric> GetRubricAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _rubric!;
    }

    public void Dispose() => _loadGate.Dispose();

    private async ValueTask EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_bank is not null && _rubric is not null)
        {
            return;
        }

        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            if (_bank is not null && _rubric is not null)
            {
                return;
            }

            byte[] questionBytes = await ReadBytesAsync(_questionsPath, cancellationToken);
            byte[] keyBytes = await ReadBytesAsync(_answerKeyPath, cancellationToken);
            byte[] rubricBytes = await ReadBytesAsync(_rubricPath, cancellationToken);
            DiagnosticBank bank = ParseQuestions(questionBytes);
            IReadOnlyDictionary<string, string> expectedOptions = ParseAnswerKey(keyBytes, bank);
            DiagnosticScoringRubric rubric = ParseRubric(rubricBytes, keyBytes, bank, expectedOptions);
            _bank = bank;
            _rubric = rubric;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private static DiagnosticBank ParseQuestions(byte[] bytes)
    {
        using JsonDocument document = JsonDocument.Parse(bytes, JsonOptions);
        JsonElement root = document.RootElement;
        EnsureObjectProperties(
            root,
            ["schemaVersion", "id", "version", "title", "questions"],
            "banque publique");
        if (root.GetProperty("schemaVersion").GetInt32() != 1)
        {
            throw new InvalidDataException("La version de schéma de la banque de diagnostic est inconnue.");
        }

        string id = RequireText(root, "id", 80);
        int version = root.GetProperty("version").GetInt32();
        if (version < 1)
        {
            throw new InvalidDataException("La version de banque doit être positive.");
        }

        string title = RequireText(root, "title", 160);
        JsonElement questionArray = root.GetProperty("questions");
        if (questionArray.ValueKind != JsonValueKind.Array
            || questionArray.GetArrayLength() != ExpectedQuestionCount)
        {
            throw new InvalidDataException($"La banque doit contenir exactement {ExpectedQuestionCount} questions.");
        }

        var questions = new List<DiagnosticQuestion>(ExpectedQuestionCount);
        var questionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement questionElement in questionArray.EnumerateArray())
        {
            EnsureObjectProperties(
                questionElement,
                ["id", "domain", "difficulty", "prompt", "options"],
                "question");
            string questionId = RequireText(questionElement, "id", 100);
            if (!questionIds.Add(questionId))
            {
                throw new InvalidDataException("Un identifiant de question est dupliqué.");
            }

            string domainId = RequireText(questionElement, "domain", 32);
            if (!DiagnosticDomains.TryParse(domainId, out DiagnosticDomain domain))
            {
                throw new InvalidDataException("Une question cible un domaine inconnu.");
            }

            int difficulty = questionElement.GetProperty("difficulty").GetInt32();
            if (difficulty is < 1 or > 3)
            {
                throw new InvalidDataException("La difficulté d'une question doit être comprise entre 1 et 3.");
            }

            string prompt = RequireText(questionElement, "prompt", 600);
            JsonElement options = questionElement.GetProperty("options");
            if (options.ValueKind != JsonValueKind.Array || options.GetArrayLength() != 4)
            {
                throw new InvalidDataException("Chaque question doit proposer exactement quatre réponses.");
            }

            var parsedOptions = new List<DiagnosticOption>(4);
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement optionElement in options.EnumerateArray())
            {
                EnsureObjectProperties(optionElement, ["id", "text"], "option");
                string optionId = RequireText(optionElement, "id", 32);
                if (!optionIds.Add(optionId))
                {
                    throw new InvalidDataException("Un identifiant de réponse est dupliqué.");
                }

                parsedOptions.Add(new DiagnosticOption(optionId, RequireText(optionElement, "text", 300)));
            }

            questions.Add(new DiagnosticQuestion(
                questionId,
                domain,
                difficulty,
                prompt,
                Array.AsReadOnly(parsedOptions.ToArray())));
        }

        string revision = Convert.ToHexString(SHA256.HashData(bytes));
        var bank = new DiagnosticBank(
            id,
            version,
            revision,
            title,
            Array.AsReadOnly(questions.ToArray()));
        _ = DiagnosticSampler.CreatePlan(bank, DiagnosticMode.Initial, seed: 0);
        return bank;
    }

    private static ReadOnlyDictionary<string, string> ParseAnswerKey(byte[] bytes, DiagnosticBank bank)
    {
        using JsonDocument document = JsonDocument.Parse(bytes, JsonOptions);
        JsonElement root = document.RootElement;
        EnsureObjectProperties(root, ["schemaVersion", "bankId", "bankVersion", "answers"], "clé privée");
        if (root.GetProperty("schemaVersion").GetInt32() != 1
            || !string.Equals(RequireText(root, "bankId", 80), bank.Id, StringComparison.Ordinal)
            || root.GetProperty("bankVersion").GetInt32() != bank.Version)
        {
            throw new InvalidDataException("La clé privée ne correspond pas à la banque publique.");
        }

        JsonElement answerArray = root.GetProperty("answers");
        if (answerArray.ValueKind != JsonValueKind.Array
            || answerArray.GetArrayLength() != bank.Questions.Count)
        {
            throw new InvalidDataException("La clé privée ne couvre pas toutes les questions.");
        }

        var answers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonElement answerElement in answerArray.EnumerateArray())
        {
            EnsureObjectProperties(answerElement, ["questionId", "expectedOptionId"], "réponse privée");
            string questionId = RequireText(answerElement, "questionId", 100);
            string expectedOptionId = RequireText(answerElement, "expectedOptionId", 32);
            if (!answers.TryAdd(questionId, expectedOptionId))
            {
                throw new InvalidDataException("La clé privée contient un doublon.");
            }

            DiagnosticQuestion question = bank.Questions.SingleOrDefault(item =>
                string.Equals(item.Id, questionId, StringComparison.Ordinal))
                ?? throw new InvalidDataException("La clé privée référence une question inconnue.");
            if (!question.Options.Any(option => string.Equals(option.Id, expectedOptionId, StringComparison.Ordinal)))
            {
                throw new InvalidDataException("La clé privée référence une option inconnue.");
            }
        }

        return new ReadOnlyDictionary<string, string>(answers);
    }

    private static DiagnosticScoringRubric ParseRubric(
        byte[] rubricBytes,
        byte[] keyBytes,
        DiagnosticBank bank,
        IReadOnlyDictionary<string, string> expectedOptions)
    {
        using JsonDocument document = JsonDocument.Parse(rubricBytes, JsonOptions);
        JsonElement root = document.RootElement;
        EnsureObjectProperties(
            root,
            [
                "schemaVersion",
                "id",
                "version",
                "bankId",
                "bankVersion",
                "difficultyWeights",
                "domains",
                "thresholds",
                "wilsonZ",
            ],
            "barème");
        if (root.GetProperty("schemaVersion").GetInt32() != 1
            || !string.Equals(RequireText(root, "bankId", 80), bank.Id, StringComparison.Ordinal)
            || root.GetProperty("bankVersion").GetInt32() != bank.Version)
        {
            throw new InvalidDataException("Le barème ne correspond pas à la banque publique.");
        }

        string id = RequireText(root, "id", 80);
        int version = root.GetProperty("version").GetInt32();
        if (version < 1)
        {
            throw new InvalidDataException("La version du barème doit être positive.");
        }

        JsonElement difficultyArray = root.GetProperty("difficultyWeights");
        if (difficultyArray.ValueKind != JsonValueKind.Array || difficultyArray.GetArrayLength() != 3)
        {
            throw new InvalidDataException("Le barème doit pondérer les trois difficultés.");
        }

        var difficultyWeights = new List<DiagnosticDifficultyWeight>(3);
        var difficulties = new HashSet<int>();
        foreach (JsonElement item in difficultyArray.EnumerateArray())
        {
            EnsureObjectProperties(item, ["difficulty", "weight"], "poids de difficulté");
            int difficulty = item.GetProperty("difficulty").GetInt32();
            decimal weight = item.GetProperty("weight").GetDecimal();
            if (difficulty is < 1 or > 3 || weight is <= 0m or > 10m || !difficulties.Add(difficulty))
            {
                throw new InvalidDataException("Un poids de difficulté est invalide.");
            }

            difficultyWeights.Add(new DiagnosticDifficultyWeight(difficulty, weight));
        }

        JsonElement domainArray = root.GetProperty("domains");
        if (domainArray.ValueKind != JsonValueKind.Array
            || domainArray.GetArrayLength() != DiagnosticDomains.All.Count)
        {
            throw new InvalidDataException("Le barème doit pondérer les neuf domaines.");
        }

        var domainWeights = new List<DiagnosticDomainWeight>(DiagnosticDomains.All.Count);
        var domains = new HashSet<DiagnosticDomain>();
        foreach (JsonElement item in domainArray.EnumerateArray())
        {
            EnsureObjectProperties(item, ["id", "weight", "critical"], "poids de domaine");
            if (!DiagnosticDomains.TryParse(RequireText(item, "id", 32), out DiagnosticDomain domain)
                || !domains.Add(domain))
            {
                throw new InvalidDataException("Un domaine du barème est invalide ou dupliqué.");
            }

            decimal weight = item.GetProperty("weight").GetDecimal();
            if (weight is <= 0m or > 5m)
            {
                throw new InvalidDataException("Un poids de domaine est invalide.");
            }

            domainWeights.Add(new DiagnosticDomainWeight(
                domain,
                weight,
                item.GetProperty("critical").GetBoolean()));
        }

        JsonElement thresholds = root.GetProperty("thresholds");
        EnsureObjectProperties(
            thresholds,
            ["criticalGapScore", "developingLowerBound", "operationalLowerBound", "strongLowerBound"],
            "seuils du barème");
        decimal criticalGapScore = thresholds.GetProperty("criticalGapScore").GetDecimal();
        decimal developingLowerBound = thresholds.GetProperty("developingLowerBound").GetDecimal();
        decimal operationalLowerBound = thresholds.GetProperty("operationalLowerBound").GetDecimal();
        decimal strongLowerBound = thresholds.GetProperty("strongLowerBound").GetDecimal();
        double wilsonZ = root.GetProperty("wilsonZ").GetDouble();
        if (criticalGapScore is < 0m or > 100m
            || developingLowerBound is < 0m or > 100m
            || operationalLowerBound <= developingLowerBound
            || strongLowerBound <= operationalLowerBound
            || strongLowerBound > 100m
            || wilsonZ is < 1d or > 3d)
        {
            throw new InvalidDataException("Les seuils ou l'intervalle du barème sont invalides.");
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(rubricBytes);
        hash.AppendData([0]);
        hash.AppendData(keyBytes);
        string revision = Convert.ToHexString(hash.GetHashAndReset());
        var snapshot = new DiagnosticRubricSnapshot(
            id,
            version,
            revision,
            bank.Id,
            bank.Version,
            bank.Revision,
            Array.AsReadOnly(difficultyWeights.OrderBy(item => item.Difficulty).ToArray()),
            Array.AsReadOnly(domainWeights.OrderBy(item => item.Domain).ToArray()),
            criticalGapScore,
            developingLowerBound,
            operationalLowerBound,
            strongLowerBound,
            wilsonZ);
        var rubric = new DiagnosticScoringRubric(snapshot, expectedOptions);
        _ = DiagnosticEvaluationRules.Evaluate(
            DiagnosticSampler.CreatePlan(bank, DiagnosticMode.Reduced, seed: 0),
            Array.Empty<DiagnosticEvaluationAnswer>(),
            rubric);
        return rubric;
    }

    private static void EnsureObjectProperties(
        JsonElement element,
        IReadOnlyCollection<string> allowed,
        string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Le document de {context} doit être un objet JSON.");
        }

        var properties = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (properties.Any(property => !allowed.Contains(property, StringComparer.Ordinal))
            || allowed.Any(property => !properties.Contains(property, StringComparer.Ordinal)))
        {
            throw new InvalidDataException($"Les propriétés du document de {context} sont invalides.");
        }
    }

    private static string RequireText(JsonElement element, string propertyName, int maximumLength)
    {
        string? value = element.GetProperty(propertyName).GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
        {
            throw new InvalidDataException($"La propriété {propertyName} est vide ou trop longue.");
        }

        return value;
    }

    private static string ResolveFile(string directory, string fileName, string contentRoot)
    {
        if (Path.IsPathFullyQualified(fileName)
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
            || fileName.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Un nom de fichier de diagnostic est interdit.");
        }

        string path = Path.GetFullPath(fileName, directory);
        EnsureContained(path, directory);
        EnsureContained(path, contentRoot);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Un fichier de diagnostic est introuvable.", path);
        }

        EnsureNoReparsePoint(path, contentRoot);
        return path;
    }

    private static async Task<byte[]> ReadBytesAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (info.Length is <= 0 or > MaximumFileBytes)
        {
            throw new InvalidDataException("Un fichier de diagnostic possède une taille interdite.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        _ = StrictUtf8.GetString(bytes);
        return bytes;
    }

    private static void EnsureContained(string candidate, string root)
    {
        string canonicalRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string canonicalCandidate = Path.GetFullPath(candidate);
        if (!canonicalCandidate.Equals(canonicalRoot, PathComparison)
            && !canonicalCandidate.StartsWith($"{canonicalRoot}{Path.DirectorySeparatorChar}", PathComparison))
        {
            throw new InvalidDataException("Un chemin de diagnostic sort de la racine autorisée.");
        }
    }

    private static void EnsureNoReparsePoint(string path, string stopAt)
    {
        string canonicalStop = Path.TrimEndingDirectorySeparator(Path.GetFullPath(stopAt));
        for (FileSystemInfo? item = new FileInfo(path); item is not null; item = item switch
        {
            FileInfo file => file.Directory,
            DirectoryInfo directory => directory.Parent,
            _ => null,
        })
        {
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Les points de réanalyse sont interdits dans le diagnostic.");
            }

            if (string.Equals(item.FullName, canonicalStop, PathComparison))
            {
                return;
            }
        }

        throw new InvalidDataException("La racine de diagnostic n'a pas été atteinte.");
    }

    private static JsonDocumentOptions JsonOptions => new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32,
    };

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
