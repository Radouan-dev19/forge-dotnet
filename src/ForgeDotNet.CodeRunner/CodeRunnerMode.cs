using ForgeDotNet.Application.CodeRunner;

namespace ForgeDotNet.CodeRunner;

public enum CodeRunnerMode
{
    Manual,
    Deterministic,
    Docker,
}

/// <summary>
/// Mode réellement configuré pour cette installation, exposé à l'interface.
/// </summary>
/// <remarks>
/// Une énumération ne peut pas être enregistrée telle quelle dans le conteneur, et l'interface doit
/// pouvoir décrire l'installation exécutée plutôt qu'un mode supposé : la page Pratique affirmait
/// qu'aucun code n'était jamais exécuté, ce qui est faux d'une installation avec runner Docker.
/// </remarks>
public sealed record CodeRunnerModeDescriptor(CodeRunnerMode Mode)
{
    public bool IsManual => Mode == CodeRunnerMode.Manual;
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
