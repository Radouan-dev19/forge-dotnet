using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Application.Diagnostic;

public sealed record DiagnosticEvaluationData(
    Guid SessionId,
    Guid ProfileId,
    DiagnosticEvaluationReport Report,
    DateTimeOffset CreatedAtUtc);

public interface IDiagnosticEvaluationRepository
{
    ValueTask<DiagnosticEvaluationData?> GetAsync(
        Guid profileId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    ValueTask<DiagnosticEvaluationData> CreateOrGetAsync(
        DiagnosticEvaluationData evaluation,
        CancellationToken cancellationToken = default);
}
