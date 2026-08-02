using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Application.CodeRunner;

namespace ForgeDotNet.CodeRunner;

public enum DeterministicRunScenario
{
    Successful,
    CompilationFailure,
    VisibleTestFailure,
    HiddenTestFailure,
    TimedOut,
    WaitForCancellation,
    Unavailable,
    LargeOutput,
}

public sealed record DeterministicCodeRunnerOptions
{
    public IReadOnlyList<DeterministicRunScenario> Scenarios { get; init; } =
        Array.AsReadOnly([DeterministicRunScenario.Unavailable]);

    public TimeSpan Delay { get; init; } = TimeSpan.Zero;

    public static IReadOnlyList<DeterministicRunScenario> ParseScenarios(string? configuredValue)
    {
        string value = string.IsNullOrWhiteSpace(configuredValue) ? "Unavailable" : configuredValue;
        string[] names = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length is < 1 or > 32)
        {
            throw new InvalidDataException("La configuration du double runner doit contenir entre 1 et 32 scénarios.");
        }

        var scenarios = new DeterministicRunScenario[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            if (!Enum.TryParse(names[index], ignoreCase: true, out DeterministicRunScenario scenario)
                || !Enum.IsDefined(scenario))
            {
                throw new InvalidDataException($"Le scénario déterministe '{names[index]}' est inconnu.");
            }

            scenarios[index] = scenario;
        }

        return Array.AsReadOnly(scenarios);
    }

    public void Validate()
    {
        if (Scenarios is null || Scenarios.Count is < 1 or > 32 || Scenarios.Any(scenario => !Enum.IsDefined(scenario)))
        {
            throw new InvalidDataException("La séquence de scénarios déterministes est invalide.");
        }

        if (Delay < TimeSpan.Zero || Delay > TimeSpan.FromSeconds(30))
        {
            throw new InvalidDataException("Le délai du double runner doit être compris entre 0 et 30 secondes.");
        }
    }
}

public sealed class DeterministicCodeRunner : ICodeRunner
{
    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();
    private readonly DeterministicCodeRunnerOptions _options;
    private readonly TimeProvider _timeProvider;
    private int _invocationCount;
    private int _scenarioIndex = -1;

    public DeterministicCodeRunner(
        DeterministicCodeRunnerOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        options.Validate();
        _options = options;
        _timeProvider = timeProvider;
    }

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public async ValueTask<CodeRunResult> RunAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        CodeRunContract.ValidateRequest(request);
        string fingerprint = ComputeFingerprint(request);
        var candidate = new CacheEntry(
            fingerprint,
            new Lazy<Task<CodeRunResult>>(
                () => ExecuteAsync(request, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));
        CacheEntry entry = _cache.GetOrAdd(request.RequestId, candidate);
        if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Un identifiant de requête runner ne peut pas être réutilisé avec un autre contenu.");
        }

        return await entry.Execution.Value;
    }

    private async Task<CodeRunResult> ExecuteAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);
        int index = Interlocked.Increment(ref _scenarioIndex);
        DeterministicRunScenario scenario = _options.Scenarios[index % _options.Scenarios.Count];
        DateTimeOffset startedAtUtc = _timeProvider.GetUtcNow();

        try
        {
            if (scenario == DeterministicRunScenario.WaitForCancellation)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, _timeProvider, cancellationToken);
            }
            else if (_options.Delay > TimeSpan.Zero)
            {
                await Task.Delay(_options.Delay, _timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CodeRunContract.NormalizeResult(
                request,
                CreateTerminalResult(
                    request,
                    CodeRunStatus.Cancelled,
                    CodeRunStageStatus.Cancelled,
                    "Simulation annulée.",
                    "La simulation a été annulée ; aucune validation automatique n'a eu lieu.",
                    startedAtUtc));
        }

        CodeRunResult result = scenario switch
        {
            DeterministicRunScenario.Successful => CreateSuccess(request, startedAtUtc, largeOutput: false),
            DeterministicRunScenario.CompilationFailure => CreateCompilationFailure(request, startedAtUtc),
            DeterministicRunScenario.VisibleTestFailure => CreateVisibleTestFailure(request, startedAtUtc),
            DeterministicRunScenario.HiddenTestFailure => CreateHiddenTestFailure(request, startedAtUtc),
            DeterministicRunScenario.TimedOut => CreateTerminalResult(
                request,
                CodeRunStatus.TimedOut,
                CodeRunStageStatus.TimedOut,
                "Délai simulé atteint.",
                "La simulation a atteint son délai ; aucune validation automatique n'a eu lieu.",
                startedAtUtc),
            DeterministicRunScenario.Unavailable => CreateTerminalResult(
                request,
                CodeRunStatus.Unavailable,
                CodeRunStageStatus.Unavailable,
                "Aucun adaptateur d'exécution isolée n'est disponible.",
                "Runner indisponible ; aucune validation automatique n'a eu lieu.",
                startedAtUtc),
            DeterministicRunScenario.LargeOutput => CreateSuccess(request, startedAtUtc, largeOutput: true),
            DeterministicRunScenario.WaitForCancellation => throw new InvalidOperationException(
                "Le scénario d'annulation doit être interrompu par un jeton d'annulation."),
            _ => throw new InvalidDataException("Le scénario déterministe est invalide."),
        };
        return CodeRunContract.NormalizeResult(request, result);
    }

    private CodeRunResult CreateSuccess(
        CodeRunRequest request,
        DateTimeOffset startedAtUtc,
        bool largeOutput)
    {
        string compilationOutput = largeOutput
            ? new string('X', CodeRunContract.MaximumOutputBytes + 2_048)
            : "Compilation simulée réussie par le double déterministe.";
        return new CodeRunResult(
            request.RequestId,
            CodeRunStatus.Succeeded,
            new CodeCompilationResult(
                CodeRunStageStatus.Succeeded,
                Array.Empty<CodeRunnerDiagnostic>(),
                new CodeRunTextOutput(compilationOutput, IsTruncated: false)),
            new CodeTestResult(
                CodeRunStageStatus.Succeeded,
                TotalCount: 4,
                PassedCount: 4,
                FailedCount: 0,
                HiddenFailureCount: 0,
                HiddenFailuresRedacted: false,
                Array.Empty<VisibleTestFailure>(),
                new CodeRunTextOutput("Tests simulés : 4 réussis, 0 échec.", IsTruncated: false)),
            "Simulation compilation/tests réussie ; ce résultat ne constitue pas une validation réelle.",
            Guid.NewGuid(),
            startedAtUtc,
            _timeProvider.GetUtcNow());
    }

    private CodeRunResult CreateCompilationFailure(CodeRunRequest request, DateTimeOffset startedAtUtc) => new(
        request.RequestId,
        CodeRunStatus.CompilationFailed,
        new CodeCompilationResult(
            CodeRunStageStatus.Failed,
            Array.AsReadOnly([
                new CodeRunnerDiagnostic(
                    "CS1002",
                    CodeRunnerDiagnosticSeverity.Error,
                    "; attendu.",
                    request.SourceFiles[0].FileName,
                    Line: 1,
                    Column: 24),
            ]),
            new CodeRunTextOutput("Échec de compilation simulé.", IsTruncated: false)),
        NotRunTests(),
        "La compilation simulée a échoué ; les tests n'ont pas été lancés.",
        Guid.NewGuid(),
        startedAtUtc,
        _timeProvider.GetUtcNow());

    private CodeRunResult CreateVisibleTestFailure(CodeRunRequest request, DateTimeOffset startedAtUtc) => new(
        request.RequestId,
        CodeRunStatus.TestsFailed,
        SuccessfulCompilation(),
        new CodeTestResult(
            CodeRunStageStatus.Failed,
            TotalCount: 3,
            PassedCount: 2,
            FailedCount: 1,
            HiddenFailureCount: 0,
            HiddenFailuresRedacted: false,
            Array.AsReadOnly([
                new VisibleTestFailure(
                    "CalculateTotal_ReturnsExpectedSum",
                    "Le résultat simulé diffère de la valeur attendue par le test visible."),
            ]),
            new CodeRunTextOutput("Tests simulés : 2 réussis, 1 échec visible.", IsTruncated: false)),
        "La compilation simulée a réussi, puis un test visible simulé a échoué.",
        Guid.NewGuid(),
        startedAtUtc,
        _timeProvider.GetUtcNow());

    private CodeRunResult CreateHiddenTestFailure(CodeRunRequest request, DateTimeOffset startedAtUtc) => new(
        request.RequestId,
        CodeRunStatus.TestsFailed,
        SuccessfulCompilation(),
        new CodeTestResult(
            CodeRunStageStatus.Failed,
            TotalCount: 4,
            PassedCount: 3,
            FailedCount: 1,
            HiddenFailureCount: 1,
            HiddenFailuresRedacted: true,
            Array.Empty<VisibleTestFailure>(),
            new CodeRunTextOutput(
                "Un test caché simulé a échoué. Son nom, son code et son diagnostic restent masqués.",
                IsTruncated: false)),
        "La compilation simulée a réussi ; un échec caché simulé est signalé sans détail sensible.",
        Guid.NewGuid(),
        startedAtUtc,
        _timeProvider.GetUtcNow());

    private CodeRunResult CreateTerminalResult(
        CodeRunRequest request,
        CodeRunStatus status,
        CodeRunStageStatus compilationStatus,
        string output,
        string summary,
        DateTimeOffset startedAtUtc) => new(
        request.RequestId,
        status,
        new CodeCompilationResult(
            compilationStatus,
            Array.Empty<CodeRunnerDiagnostic>(),
            new CodeRunTextOutput(output, IsTruncated: false)),
        NotRunTests(),
        summary,
        Guid.NewGuid(),
        startedAtUtc,
        _timeProvider.GetUtcNow());

    private static CodeCompilationResult SuccessfulCompilation() => new(
        CodeRunStageStatus.Succeeded,
        Array.Empty<CodeRunnerDiagnostic>(),
        new CodeRunTextOutput("Compilation simulée réussie par le double déterministe.", IsTruncated: false));

    private static CodeTestResult NotRunTests() => new(
        CodeRunStageStatus.NotRun,
        TotalCount: 0,
        PassedCount: 0,
        FailedCount: 0,
        HiddenFailureCount: 0,
        HiddenFailuresRedacted: false,
        Array.Empty<VisibleTestFailure>(),
        new CodeRunTextOutput(string.Empty, IsTruncated: false));

    private static string ComputeFingerprint(CodeRunRequest request)
    {
        var builder = new StringBuilder();
        Append(builder, request.ExerciseId);
        Append(builder, request.ExerciseVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(builder, request.ContentRevision);
        foreach (CodeRunSourceFile sourceFile in request.SourceFiles)
        {
            Append(builder, sourceFile.FileName);
            Append(builder, sourceFile.Content);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void Append(StringBuilder builder, string value) => builder
        .Append(value.Length)
        .Append(':')
        .Append(value)
        .Append('|');

    private sealed record CacheEntry(string Fingerprint, Lazy<Task<CodeRunResult>> Execution);
}
