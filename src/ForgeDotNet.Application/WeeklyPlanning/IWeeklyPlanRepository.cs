using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.Application.WeeklyPlanning;

public sealed record WeeklyPlanData(
    Guid Id,
    Guid ProfileId,
    Guid DiagnosticSessionId,
    int Version,
    WeeklyPlanStatus Status,
    WeeklyPlanSnapshot Snapshot,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? AcceptedAtUtc);

public interface IWeeklyPlanRepository
{
    ValueTask<WeeklyPlanData?> GetLatestAsync(
        Guid profileId,
        Guid diagnosticSessionId,
        CancellationToken cancellationToken = default);

    ValueTask<WeeklyPlanData> CreateInitialOrGetAsync(
        WeeklyPlanData plan,
        CancellationToken cancellationToken = default);

    ValueTask<WeeklyPlanData> CreateNextVersionAsync(
        WeeklyPlanData plan,
        int expectedPreviousVersion,
        CancellationToken cancellationToken = default);

    ValueTask<WeeklyPlanData> AcceptAsync(
        Guid profileId,
        Guid diagnosticSessionId,
        int expectedVersion,
        DateTimeOffset acceptedAtUtc,
        CancellationToken cancellationToken = default);
}
