using System.Text;
using System.Text.RegularExpressions;

namespace ForgeDotNet.Application.CodeRunner;

public enum CodeRunStatus
{
    Succeeded,
    CompilationFailed,
    TestsFailed,
    TimedOut,
    Cancelled,
    Unavailable,
}

public enum CodeRunStageStatus
{
    NotRun,
    Succeeded,
    Failed,
    TimedOut,
    Cancelled,
    Unavailable,
}

public enum CodeRunnerDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record CodeRunSourceFile(string FileName, string Content);

public sealed record CodeRunRequest(
    Guid RequestId,
    string ExerciseId,
    int ExerciseVersion,
    string ContentRevision,
    IReadOnlyList<CodeRunSourceFile> SourceFiles);

public sealed record CodeRunTextOutput(string Text, bool IsTruncated);

public sealed record CodeRunnerDiagnostic(
    string Code,
    CodeRunnerDiagnosticSeverity Severity,
    string Message,
    string? FileName,
    int? Line,
    int? Column);

public sealed record CodeCompilationResult(
    CodeRunStageStatus Status,
    IReadOnlyList<CodeRunnerDiagnostic> Diagnostics,
    CodeRunTextOutput Output);

public sealed record VisibleTestFailure(string Name, string Message);

public sealed record CodeTestResult(
    CodeRunStageStatus Status,
    int TotalCount,
    int PassedCount,
    int FailedCount,
    int HiddenFailureCount,
    bool HiddenFailuresRedacted,
    IReadOnlyList<VisibleTestFailure> VisibleFailures,
    CodeRunTextOutput Output);

public sealed record CodeRunResult(
    Guid RequestId,
    CodeRunStatus Status,
    CodeCompilationResult Compilation,
    CodeTestResult Tests,
    string Summary,
    Guid DiagnosticId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);

public interface ICodeRunner
{
    ValueTask<CodeRunResult> RunAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default);
}

public static partial class CodeRunContract
{
    public const int MaximumSourceFileCount = 8;
    public const int MaximumSourceFileBytes = 64 * 1024;
    public const int MaximumTotalSourceBytes = 256 * 1024;
    public const int MaximumOutputBytes = 64 * 1024;
    public const int MaximumDiagnosticCount = 100;
    public const int MaximumVisibleFailureCount = 100;

    private const int MaximumFileNameLength = 120;
    private const int MaximumDiagnosticTextLength = 2_000;
    private const int MaximumSummaryLength = 512;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.-]*\\.cs$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceFileNamePattern();

    /// <summary>
    /// Cible d'exécution : un identifiant d'exercice ou de scénario, ou une suite d'acceptation de
    /// projet de la forme <c>&lt;projet&gt;.&lt;jalon&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Le second segment a été omis lors de l'ajout des projets, alors que <c>SubmitProject</c>
    /// construit exactement cette forme et que <c>FileSystemProjectSource.FindSuiteAsync</c> la
    /// redécoupe sur son dernier point. Les deux moitiés de la fonctionnalité se contredisaient donc,
    /// et aucune soumission de projet ne pouvait aboutir : la validation rejetait la requête avant
    /// tout appel au bac à sable.
    ///
    /// La forme reste étroite plutôt que permissive. Un seul segment supplémentaire est admis, aucun
    /// point n'est accepté à l'intérieur d'un segment, ni en tête, ni en queue : « .. » est donc
    /// impossible par construction. Cela importe parce que l'identifiant d'un exercice sert aussi à
    /// composer un chemin sous la racine du catalogue — protégé par ailleurs par un contrôle de
    /// descendance et un refus des points de réanalyse, que cette forme ne remplace pas mais dont
    /// elle évite d'éprouver les limites.
    /// </remarks>
    [GeneratedRegex(
        "^[a-z0-9][a-z0-9-]{2,99}(\\.[a-z0-9][a-z0-9-]{0,49})?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ExerciseIdPattern();

    [GeneratedRegex("^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex RevisionPattern();

    public static void ValidateRequest(CodeRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestId == Guid.Empty)
        {
            throw new ArgumentException("L'identifiant de requête runner est obligatoire.", nameof(request));
        }

        if (!ExerciseIdPattern().IsMatch(request.ExerciseId ?? string.Empty))
        {
            throw new ArgumentException("L'identifiant d'exercice runner est invalide.", nameof(request));
        }

        if (request.ExerciseVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "La version d'exercice runner doit être positive.");
        }

        if (!RevisionPattern().IsMatch(request.ContentRevision ?? string.Empty))
        {
            throw new ArgumentException("La révision de contenu runner doit être un SHA-256.", nameof(request));
        }

        if (request.SourceFiles is null
            || request.SourceFiles.Count is < 1 or > MaximumSourceFileCount)
        {
            throw new ArgumentException(
                $"Une requête runner doit contenir entre 1 et {MaximumSourceFileCount} fichiers source.",
                nameof(request));
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalBytes = 0;
        foreach (CodeRunSourceFile sourceFile in request.SourceFiles)
        {
            ArgumentNullException.ThrowIfNull(sourceFile);
            if (string.IsNullOrWhiteSpace(sourceFile.FileName)
                || sourceFile.FileName.Length > MaximumFileNameLength
                || sourceFile.FileName.Contains("..", StringComparison.Ordinal)
                || !SourceFileNamePattern().IsMatch(sourceFile.FileName)
                || !names.Add(sourceFile.FileName))
            {
                throw new ArgumentException(
                    "Chaque fichier source doit avoir un nom simple .cs, unique et sans chemin.",
                    nameof(request));
            }

            if (sourceFile.Content is null)
            {
                throw new ArgumentException("Le contenu d'un fichier source ne peut pas être null.", nameof(request));
            }

            int fileBytes;
            try
            {
                fileBytes = StrictUtf8.GetByteCount(sourceFile.Content);
            }
            catch (EncoderFallbackException exception)
            {
                throw new ArgumentException("Le contenu source doit être un texte UTF-8 valide.", nameof(request), exception);
            }

            if (fileBytes > MaximumSourceFileBytes)
            {
                throw new ArgumentException(
                    $"Un fichier source ne peut pas dépasser {MaximumSourceFileBytes} octets.",
                    nameof(request));
            }

            totalBytes = checked(totalBytes + fileBytes);
        }

        if (totalBytes > MaximumTotalSourceBytes)
        {
            throw new ArgumentException(
                $"La requête runner ne peut pas dépasser {MaximumTotalSourceBytes} octets.",
                nameof(request));
        }
    }

    public static CodeRunResult NormalizeResult(CodeRunRequest request, CodeRunResult result)
    {
        ValidateRequest(request);
        ArgumentNullException.ThrowIfNull(result);
        if (result.RequestId != request.RequestId || result.DiagnosticId == Guid.Empty)
        {
            throw new InvalidDataException("Le résultat runner ne correspond pas à la requête ou n'est pas traçable.");
        }

        if (result.CompletedAtUtc < result.StartedAtUtc)
        {
            throw new InvalidDataException("Les horodatages du résultat runner sont incohérents.");
        }

        ValidateStatusCombination(result);
        CodeRunnerDiagnostic[] diagnostics = result.Compilation.Diagnostics
            .Take(MaximumDiagnosticCount)
            .Select(diagnostic => NormalizeDiagnostic(request, diagnostic))
            .ToArray();
        VisibleTestFailure[] failures = result.Tests.VisibleFailures
            .Take(MaximumVisibleFailureCount)
            .Select(failure => new VisibleTestFailure(
                BoundText(failure.Name, MaximumDiagnosticTextLength),
                BoundText(failure.Message, MaximumDiagnosticTextLength)))
            .ToArray();
        ValidateTestCounts(result.Tests, failures.Length);

        return result with
        {
            Compilation = result.Compilation with
            {
                Diagnostics = Array.AsReadOnly(diagnostics),
                Output = NormalizeOutput(result.Compilation.Output),
            },
            Tests = result.Tests with
            {
                VisibleFailures = Array.AsReadOnly(failures),
                Output = NormalizeOutput(result.Tests.Output),
            },
            Summary = BoundText(result.Summary, MaximumSummaryLength),
        };
    }

    public static CodeRunTextOutput BoundOutput(string? text)
    {
        text ??= string.Empty;
        if (StrictUtf8.GetByteCount(text) <= MaximumOutputBytes)
        {
            return new CodeRunTextOutput(text, IsTruncated: false);
        }

        const string suffix = "\n[… sortie tronquée …]";
        int availableBytes = MaximumOutputBytes - StrictUtf8.GetByteCount(suffix);
        var builder = new StringBuilder();
        int byteCount = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            string value = rune.ToString();
            int runeBytes = StrictUtf8.GetByteCount(value);
            if (byteCount + runeBytes > availableBytes)
            {
                break;
            }

            builder.Append(value);
            byteCount += runeBytes;
        }

        builder.Append(suffix);
        return new CodeRunTextOutput(builder.ToString(), IsTruncated: true);
    }

    private static CodeRunnerDiagnostic NormalizeDiagnostic(
        CodeRunRequest request,
        CodeRunnerDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        string? fileName = diagnostic.FileName;
        if (fileName is not null
            && !request.SourceFiles.Any(source => string.Equals(
                source.FileName,
                fileName,
                StringComparison.OrdinalIgnoreCase)))
        {
            fileName = null;
        }

        return diagnostic with
        {
            Code = BoundText(diagnostic.Code, 64),
            Message = BoundText(diagnostic.Message, MaximumDiagnosticTextLength),
            FileName = fileName,
            Line = diagnostic.Line is > 0 ? diagnostic.Line : null,
            Column = diagnostic.Column is > 0 ? diagnostic.Column : null,
        };
    }

    private static void ValidateStatusCombination(CodeRunResult result)
    {
        bool valid = result.Status switch
        {
            CodeRunStatus.Succeeded => result.Compilation.Status == CodeRunStageStatus.Succeeded
                && result.Tests.Status == CodeRunStageStatus.Succeeded,
            CodeRunStatus.CompilationFailed => result.Compilation.Status == CodeRunStageStatus.Failed
                && result.Tests.Status == CodeRunStageStatus.NotRun,
            CodeRunStatus.TestsFailed => result.Compilation.Status == CodeRunStageStatus.Succeeded
                && result.Tests.Status == CodeRunStageStatus.Failed,
            CodeRunStatus.TimedOut => (result.Compilation.Status == CodeRunStageStatus.TimedOut
                    && result.Tests.Status == CodeRunStageStatus.NotRun)
                || (result.Compilation.Status == CodeRunStageStatus.Succeeded
                    && result.Tests.Status == CodeRunStageStatus.TimedOut),
            CodeRunStatus.Cancelled => (result.Compilation.Status == CodeRunStageStatus.Cancelled
                    && result.Tests.Status == CodeRunStageStatus.NotRun)
                || (result.Compilation.Status == CodeRunStageStatus.Succeeded
                    && result.Tests.Status == CodeRunStageStatus.Cancelled),
            CodeRunStatus.Unavailable => result.Compilation.Status == CodeRunStageStatus.Unavailable
                && result.Tests.Status == CodeRunStageStatus.NotRun,
            _ => false,
        };
        if (!valid)
        {
            throw new InvalidDataException("La combinaison des statuts runner est incohérente.");
        }
    }

    private static void ValidateTestCounts(CodeTestResult tests, int visibleFailureCount)
    {
        if (tests.TotalCount < 0
            || tests.PassedCount < 0
            || tests.FailedCount < 0
            || tests.HiddenFailureCount < 0
            || tests.PassedCount + tests.FailedCount > tests.TotalCount
            || tests.HiddenFailureCount > tests.FailedCount
            || visibleFailureCount > tests.FailedCount - tests.HiddenFailureCount
            || (tests.HiddenFailureCount > 0 && !tests.HiddenFailuresRedacted)
            || (tests.Status == CodeRunStageStatus.Succeeded && tests.FailedCount != 0)
            || (tests.Status == CodeRunStageStatus.Failed && tests.FailedCount == 0)
            || (tests.Status == CodeRunStageStatus.NotRun
                && (tests.TotalCount != 0 || tests.PassedCount != 0 || tests.FailedCount != 0)))
        {
            throw new InvalidDataException("Les compteurs de tests runner sont incohérents.");
        }
    }

    private static CodeRunTextOutput NormalizeOutput(CodeRunTextOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        CodeRunTextOutput bounded = BoundOutput(output.Text);
        return bounded with { IsTruncated = bounded.IsTruncated || output.IsTruncated };
    }

    private static string BoundText(string? value, int maximumLength)
    {
        value ??= string.Empty;
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
