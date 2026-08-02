using ForgeDotNet.Application.CodeRunner;

namespace ForgeDotNet.CodeRunner;

public enum CodeRunnerMode
{
    Manual,
    Deterministic,
    Docker,
}

public static class CodeRunnerModeParser
{
    public static CodeRunnerMode Parse(string? configuredValue)
    {
        string value = string.IsNullOrWhiteSpace(configuredValue) ? "Manual" : configuredValue;
        if (!Enum.TryParse(value, ignoreCase: true, out CodeRunnerMode mode) || !Enum.IsDefined(mode))
        {
            throw new InvalidDataException("Le mode CodeRunner doit être Manual, Deterministic ou Docker.");
        }

        return mode;
    }
}

public sealed class UnavailableCodeRunner(TimeProvider timeProvider) : ICodeRunner
{
    public ValueTask<CodeRunResult> RunAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        CodeRunContract.ValidateRequest(request);
        DateTimeOffset now = timeProvider.GetUtcNow();
        var result = new CodeRunResult(
            request.RequestId,
            CodeRunStatus.Unavailable,
            new CodeCompilationResult(
                CodeRunStageStatus.Unavailable,
                Array.Empty<CodeRunnerDiagnostic>(),
                new CodeRunTextOutput(
                    "Mode manuel actif : aucun code n’a été transmis à Docker.",
                    IsTruncated: false)),
            new CodeTestResult(
                CodeRunStageStatus.NotRun,
                0,
                0,
                0,
                0,
                HiddenFailuresRedacted: false,
                Array.Empty<VisibleTestFailure>(),
                new CodeRunTextOutput(string.Empty, IsTruncated: false)),
            "Runner automatique indisponible ; utilisez l’export ZIP manuel sans preuve automatique.",
            Guid.NewGuid(),
            now,
            now);
        return ValueTask.FromResult(CodeRunContract.NormalizeResult(request, result));
    }
}
