using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.Application.Practice;
using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.CodeRunner;

public sealed class DockerRunContentOptions
{
    public required string ContentRootPath { get; init; }

    public required string CatalogDirectoryPath { get; init; }

    public string? SqlDirectoryPath { get; init; }
}

public sealed partial class FileSystemDockerRunSpecificationSource(
    IPracticeExerciseSource exerciseSource,
    DockerRunContentOptions options,
    IDebugScenarioSource? debugScenarioSource = null) : IDockerRunSpecificationSource
{
    private const int MaximumFileBytes = 128 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{2,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SuiteIdPattern();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,79}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    public async ValueTask<DockerRunSpecification?> GetAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var exercise = await exerciseSource.GetAsync(request.ExerciseId, cancellationToken);
        DebugScenario? debugScenario = exercise is null && debugScenarioSource is not null
            ? await debugScenarioSource.GetAsync(request.ExerciseId, cancellationToken)
            : null;
        string contentRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.ContentRootPath));
        string catalogRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.CatalogDirectoryPath));
        EnsureDescendant(contentRoot, catalogRoot);
        string suiteRoot;
        if (exercise is not null || debugScenario is not null)
        {
            int? contentVersion = exercise?.Version ?? debugScenario?.Version;
            string? contentRevision = exercise?.Revision ?? debugScenario?.Revision;
            if (contentVersion != request.ExerciseVersion
                || !string.Equals(contentRevision, request.ContentRevision, StringComparison.Ordinal))
            {
                return null;
            }

            string contentKind = exercise is null ? "debugging" : "exercises";
            suiteRoot = Path.GetFullPath(Path.Combine(catalogRoot, contentKind, request.ExerciseId));
            EnsureDescendant(catalogRoot, suiteRoot);
            EnsureNoReparsePoints(contentRoot, suiteRoot);
        }
        else
        {
            suiteRoot = await ResolveEfExamSuiteRootAsync(request, contentRoot, cancellationToken)
                ?? string.Empty;
            if (suiteRoot.Length == 0)
            {
                return null;
            }
        }

        RunnerSuiteFile metadata = await ReadAsync<RunnerSuiteFile>(
            contentRoot, Path.Combine(suiteRoot, "tests", "runner.json"), cancellationToken);
        RunnerCasesFile visible = await ReadAsync<RunnerCasesFile>(
            contentRoot, Path.Combine(suiteRoot, "tests", "visible", "cases.json"), cancellationToken);
        RunnerCasesFile hidden = await ReadAsync<RunnerCasesFile>(
            contentRoot, Path.Combine(suiteRoot, "tests", "hidden", "cases.json"), cancellationToken);

        ValidateMetadata(metadata, request);
        RunnerTestCase[] cases = ValidateCases(metadata, visible, hidden);
        var suite = new RunnerSuiteDefinition(
            1,
            metadata.SuiteId,
            metadata.ExerciseId,
            metadata.ExerciseVersion,
            metadata.TypeName,
            metadata.MethodName,
            Array.AsReadOnly(metadata.ParameterTypes),
            metadata.ReturnType,
            Array.AsReadOnly(cases));
        return new DockerRunSpecification(metadata.SuiteId, JsonSerializer.Serialize(suite, JsonOptions));
    }

    private async ValueTask<string?> ResolveEfExamSuiteRootAsync(
        CodeRunRequest request,
        string contentRoot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.SqlDirectoryPath))
        {
            return null;
        }

        string sqlRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.SqlDirectoryPath));
        EnsureDescendant(contentRoot, sqlRoot);
        string scenarioRoot = Path.GetFullPath(Path.Combine(sqlRoot, request.ExerciseId));
        EnsureDescendant(sqlRoot, scenarioRoot);
        if (!Directory.Exists(scenarioRoot))
        {
            return null;
        }

        EnsureNoReparsePoints(contentRoot, scenarioRoot);
        string suiteRoot = Path.Combine(scenarioRoot, "exam");
        string[] paths =
        [
            Path.Combine(scenarioRoot, "scenario.json"),
            Path.Combine(scenarioRoot, "tests", "contract.json"),
            Path.Combine(scenarioRoot, "statement.md"),
            Path.Combine(suiteRoot, "starter", "Submission.cs"),
            Path.Combine(suiteRoot, "solution", "Submission.cs"),
            Path.Combine(suiteRoot, "tests", "runner.json"),
            Path.Combine(suiteRoot, "tests", "visible", "cases.json"),
            Path.Combine(suiteRoot, "tests", "hidden", "cases.json"),
        ];
        byte[][] parts = new byte[paths.Length][];
        for (int index = 0; index < paths.Length; index++)
        {
            parts[index] = await ReadBytesAsync(contentRoot, paths[index], cancellationToken);
        }

        using JsonDocument manifest = JsonDocument.Parse(parts[0]);
        using JsonDocument contract = JsonDocument.Parse(parts[1]);
        if (!string.Equals(
                manifest.RootElement.GetProperty("id").GetString(),
                request.ExerciseId,
                StringComparison.Ordinal)
            || manifest.RootElement.GetProperty("version").GetInt32() != request.ExerciseVersion
            || !string.Equals(contract.RootElement.GetProperty("mode").GetString(), "ef", StringComparison.Ordinal)
            || !string.Equals(ComputeRevision(parts), request.ContentRevision, StringComparison.Ordinal))
        {
            return null;
        }

        return suiteRoot;
    }

    private static void ValidateMetadata(RunnerSuiteFile metadata, CodeRunRequest request)
    {
        if (metadata.SchemaVersion != 1
            || !SuiteIdPattern().IsMatch(metadata.SuiteId ?? string.Empty)
            || !string.Equals(metadata.ExerciseId, request.ExerciseId, StringComparison.Ordinal)
            || metadata.ExerciseVersion != request.ExerciseVersion
            || !string.Equals(metadata.TypeName, "Submission", StringComparison.Ordinal)
            || !IdentifierPattern().IsMatch(metadata.MethodName ?? string.Empty)
            || metadata.ParameterTypes is null
            || metadata.ParameterTypes.Length > 8)
        {
            throw new InvalidDataException("Le contrat de suite runner est invalide.");
        }

        foreach (string type in metadata.ParameterTypes.Append(metadata.ReturnType))
        {
            _ = RunnerTypeCatalog.Resolve(type);
        }
    }

    private static RunnerTestCase[] ValidateCases(
        RunnerSuiteFile metadata,
        RunnerCasesFile visible,
        RunnerCasesFile hidden)
    {
        if (visible.SchemaVersion != 1 || hidden.SchemaVersion != 1
            || visible.Cases is null || visible.Cases.Count is < 1 or > 20
            || hidden.Cases is null || hidden.Cases.Count is < 1 or > 20)
        {
            throw new InvalidDataException("Le volume de cas runner est invalide.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        return visible.Cases.Select(item => Convert(item, true))
            .Concat(hidden.Cases.Select(item => Convert(item, false)))
            .ToArray();

        RunnerTestCase Convert(RunnerCaseFile item, bool isVisible)
        {
            string caseName = item.Name ?? string.Empty;
            bool hasExpected = item.Expected.ValueKind != JsonValueKind.Undefined;
            bool hasException = !string.IsNullOrWhiteSpace(item.ExpectedException);
            if (!IdentifierPattern().IsMatch(caseName)
                || !names.Add(caseName)
                || string.IsNullOrWhiteSpace(item.Message) || item.Message.Length > 300
                || item.Arguments.ValueKind != JsonValueKind.Array
                || item.Arguments.GetArrayLength() != metadata.ParameterTypes.Length
                || hasExpected == hasException
                || (hasException && item.ExpectedException is not (
                    "ArgumentException" or "ArgumentNullException" or "ArgumentOutOfRangeException")))
            {
                throw new InvalidDataException("Un cas runner est invalide.");
            }

            int index = 0;
            foreach (JsonElement argument in item.Arguments.EnumerateArray())
            {
                _ = argument.Deserialize(RunnerTypeCatalog.Resolve(metadata.ParameterTypes[index++]), JsonOptions);
            }

            JsonElement expected = hasExpected
                ? item.Expected.Clone()
                : JsonDocument.Parse("null").RootElement.Clone();
            if (hasExpected)
            {
                _ = expected.Deserialize(RunnerTypeCatalog.Resolve(metadata.ReturnType), JsonOptions);
            }

            return new RunnerTestCase(
                caseName,
                item.Message,
                isVisible,
                item.Arguments.Clone(),
                hasExpected,
                expected,
                item.ExpectedException,
                item.ArgumentsUnchanged);
        }
    }

    private static async Task<T> ReadAsync<T>(string contentRoot, string path, CancellationToken cancellationToken)
    {
        byte[] bytes = await ReadBytesAsync(contentRoot, path, cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<T>(StrictUtf8.GetString(bytes), JsonOptions)
                ?? throw new InvalidDataException("Un fichier de suite runner est vide.");
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            throw new InvalidDataException("Un fichier de suite runner est invalide.", exception);
        }
    }

    private static async Task<byte[]> ReadBytesAsync(
        string contentRoot,
        string path,
        CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(path);
        EnsureDescendant(contentRoot, fullPath);
        FileInfo info = new(fullPath);
        if (!info.Exists || info.Length is <= 0 or > MaximumFileBytes)
        {
            throw new InvalidDataException("Un fichier de suite runner est absent ou trop volumineux.");
        }

        EnsureNoReparsePoints(contentRoot, fullPath);
        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    private static string ComputeRevision(IEnumerable<byte[]> parts)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (byte[] part in parts)
        {
            hash.AppendData(part);
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
            throw new InvalidDataException("Un chemin de suite runner sort de content/.");
        }
    }

    private static void EnsureNoReparsePoints(string contentRoot, string target)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRoot));
        string current = root;
        foreach (string segment in Path.GetRelativePath(root, target)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Les points de réanalyse sont interdits dans une suite runner.");
            }
        }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private sealed record RunnerSuiteFile(
        int SchemaVersion,
        string SuiteId,
        string ExerciseId,
        int ExerciseVersion,
        string TypeName,
        string MethodName,
        string[] ParameterTypes,
        string ReturnType);

    private sealed record RunnerCasesFile(int SchemaVersion, List<RunnerCaseFile> Cases);

    private sealed record RunnerCaseFile(
        string Name,
        string Message,
        JsonElement Arguments,
        JsonElement Expected,
        string? ExpectedException,
        bool ArgumentsUnchanged);
}
