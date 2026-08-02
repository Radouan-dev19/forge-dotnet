using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Application.Diagnostic;

public sealed record DiagnosticResponseData(
    string QuestionId,
    string SelectedOptionId,
    DateTimeOffset SavedAtUtc);

public sealed record DiagnosticSessionData(
    Guid Id,
    Guid ProfileId,
    string BankId,
    int BankVersion,
    string BankRevision,
    DiagnosticPlan Plan,
    DiagnosticTimeline Timeline,
    int SectionDurationSeconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? EndedAtUtc,
    IReadOnlyList<DiagnosticResponseData> Responses);

public interface IDiagnosticSessionRepository
{
    ValueTask<DiagnosticSessionData?> GetAsync(
        Guid profileId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    ValueTask<DiagnosticSessionData?> GetActiveAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    ValueTask<DiagnosticSessionData?> GetLatestAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    ValueTask<DiagnosticSessionData> CreateOrGetActiveAsync(
        DiagnosticSessionData session,
        CancellationToken cancellationToken = default);

    ValueTask SaveTimelineAsync(
        Guid profileId,
        Guid sessionId,
        DiagnosticTimeline timeline,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? endedAtUtc,
        CancellationToken cancellationToken = default);

    ValueTask UpsertResponseAsync(
        Guid profileId,
        Guid sessionId,
        DiagnosticResponseData response,
        CancellationToken cancellationToken = default);
}
