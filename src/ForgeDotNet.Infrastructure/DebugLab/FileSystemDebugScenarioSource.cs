using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.Infrastructure.DebugLab;

public sealed partial class FileSystemDebugScenarioSource(
    ContentCatalogProvider catalogProvider,
    DebugContentOptions options) : IDebugScenarioSource
{
    private const int MaximumFileBytes = 128 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    [GeneratedRegex("(?:[A-Za-z]:\\\\|/Users/|/home/|/workspace/|/input/|Bearer\\s+|api[_-]?key|password|secret)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveLogPattern();

    public async ValueTask<IReadOnlyList<DebugScenario>> ListAsync(CancellationToken cancellationToken = default)
    {
        var scenarios = new List<DebugScenario>();
        foreach (ContentCatalogItem item in catalogProvider.Current.GetByType(ContentDocumentType.DebugScenario))
        {
            DebugScenario? scenario = await GetAsync(item.Id, cancellationToken);
            if (scenario is not null) scenarios.Add(scenario);
        }
        return Array.AsReadOnly(scenarios.ToArray());
    }

    public async ValueTask<DebugScenario?> GetAsync(string scenarioId, CancellationToken cancellationToken = default)
    {
        ContentCatalogItem? catalogItem = catalogProvider.Current.FindById(scenarioId);
        if (catalogItem is null || catalogItem.Type != ContentDocumentType.DebugScenario) return null;

        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string catalogRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.CatalogDirectoryPath));
        EnsureDescendant(contentRoot, catalogRoot);
        string scenarioRoot = Path.GetFullPath(Path.Combine(catalogRoot, "debugging", scenarioId));
        EnsureDescendant(catalogRoot, scenarioRoot);
        EnsureNoReparsePoints(contentRoot, scenarioRoot);

        LoadedFile manifestFile = await ReadFileAsync(contentRoot, scenarioRoot, "scenario.json", cancellationToken);
        using JsonDocument manifest = ParseJson(manifestFile.Text);
        JsonElement root = manifest.RootElement;
        string id = ReadString(root, "id");
        int version = root.GetProperty("version").GetInt32();
        if (id != catalogItem.Id || version != catalogItem.Version)
        {
            throw new InvalidDataException("Le scénario privé ne correspond pas au catalogue public.");
        }

        LoadedFile ticket = await ReadFileAsync(contentRoot, scenarioRoot, ReadString(root, "ticketPath"), cancellationToken);
        LoadedFile logs = await ReadFileAsync(contentRoot, scenarioRoot, ReadString(root, "logsPath"), cancellationToken);
        if (SensitiveLogPattern().IsMatch(logs.Text))
        {
            throw new InvalidDataException("Les journaux initiaux contiennent un chemin hôte ou une donnée sensible.");
        }

        LoadedFile broken = await ReadFileAsync(
            contentRoot, scenarioRoot, ReadString(root, "brokenRepositoryPath") + "Submission.cs", cancellationToken);
        LoadedFile correction = await ReadFileAsync(
            contentRoot, scenarioRoot, ReadString(root, "correctionPath") + "Submission.cs", cancellationToken);
        LoadedFile regression = await ReadFileAsync(
            contentRoot, scenarioRoot, ReadString(root, "regressionTestPath"), cancellationToken);
        LoadedFile rubricFile = await ReadFileAsync(contentRoot, scenarioRoot, "tests/rubric.json", cancellationToken);
        RubricFile rubric = JsonSerializer.Deserialize<RubricFile>(rubricFile.Text, JsonOptions)
            ?? throw new InvalidDataException("La grille DebugLab est vide.");
        if (rubric.SchemaVersion != 1 || rubric.ScenarioId != id || rubric.Criteria is null || rubric.Criteria.Count is < 2 or > 8)
        {
            throw new InvalidDataException("La grille DebugLab ne correspond pas au scénario.");
        }

        LoadedFile runner = await ReadFileAsync(contentRoot, scenarioRoot, "tests/runner.json", cancellationToken);
        LoadedFile visible = await ReadFileAsync(contentRoot, scenarioRoot, "tests/visible/cases.json", cancellationToken);
        LoadedFile hidden = await ReadFileAsync(contentRoot, scenarioRoot, "tests/hidden/cases.json", cancellationToken);
        LoadedFile[] revisionFiles = [manifestFile, ticket, logs, broken, correction, regression, rubricFile, runner, visible, hidden];
        var scenario = new DebugScenario(
            id,
            version,
            ComputeRevision(revisionFiles),
            ReadString(root, "title"),
            root.GetProperty("difficulty").GetInt32(),
            root.GetProperty("estimatedMinutes").GetInt32(),
            Array.AsReadOnly(root.GetProperty("skills").EnumerateArray().Select(item => item.GetString()!).ToArray()),
            ticket.Text,
            ReadString(root, "expectedBehavior"),
            logs.Text,
            Array.AsReadOnly(root.GetProperty("checklist").EnumerateArray().Select(item => item.GetString()!).ToArray()),
            Array.AsReadOnly(root.GetProperty("observationQuestions").EnumerateArray().Select(item => item.GetString()!).ToArray()),
            broken.Text,
            correction.Text,
            regression.Text,
            Array.AsReadOnly(rubric.Criteria.Select(item => new DebugRubricCriterion(
                item.Id, item.Label, item.JournalField,
                Array.AsReadOnly(item.RequiredTerms.ToArray()), item.MinimumMatches)).ToArray()));
        DebugLabRules.ValidateScenario(scenario);
        return scenario;
    }

    private static async Task<LoadedFile> ReadFileAsync(
        string contentRoot,
        string scenarioRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathFullyQualified(relativePath)
            || relativePath.Split('/', '\\').Any(segment => segment is "." or ".."))
        {
            throw new InvalidDataException("Un chemin privé DebugLab est invalide.");
        }
        string path = Path.GetFullPath(relativePath, scenarioRoot);
        EnsureDescendant(scenarioRoot, path);
        EnsureNoReparsePoints(contentRoot, path);
        FileInfo info = new(path);
        if (!info.Exists || info.Length is <= 0 or > MaximumFileBytes)
        {
            throw new InvalidDataException("Un fichier privé DebugLab est absent ou trop volumineux.");
        }
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        try
        {
            return new LoadedFile(Path.GetRelativePath(contentRoot, path).Replace('\\', '/'), StrictUtf8.GetString(bytes), bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Les fichiers DebugLab doivent être en UTF-8 strict.", exception);
        }
    }

    private static JsonDocument ParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 32 });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Le manifeste DebugLab est illisible.", exception);
        }
    }

    private static string ReadString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString() is { Length: > 0 } value
            ? value
            : throw new InvalidDataException("Un texte DebugLab obligatoire est absent.");

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

    private static void EnsureDescendant(string parent, string child)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        string candidate = Path.GetFullPath(child);
        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, PathComparison))
        {
            throw new InvalidDataException("Un chemin DebugLab sort de content/.");
        }
    }

    private static void EnsureNoReparsePoints(string contentRoot, string target)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRoot));
        string current = root;
        foreach (string segment in Path.GetRelativePath(root, target).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Les points de réanalyse sont interdits dans DebugLab.");
            }
        }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private sealed record LoadedFile(string RelativePath, string Text, byte[] Bytes);
    private sealed record RubricFile(int SchemaVersion, string ScenarioId, List<RubricCriterionFile> Criteria);
    private sealed record RubricCriterionFile(
        string Id, string Label, string JournalField, List<string> RequiredTerms, int MinimumMatches);
}
