using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Mastery;

public sealed record MasteryComponentView(
    MasteryComponent Component,
    string Label,
    decimal WeightPercent,
    decimal Score,
    bool HasEvidence,
    int EvidenceCount,
    int DistinctItemCount);

public sealed record MasteryDomainView(
    MasteryDomain Domain,
    string Label,
    decimal Score,
    decimal RequiredScore,
    bool IsCritical,
    bool IsValidated,
    IReadOnlyList<MasteryComponentView> Components,
    IReadOnlyList<string> Blockers);

public sealed record MasteryGateView(
    MasteryGate Gate,
    string Label,
    bool IsOpen,
    IReadOnlyList<string> Blockers);

public sealed record MasteryDashboardView(
    string PolicyId,
    int PolicyVersion,
    string PolicyRevision,
    string EvidenceRevision,
    DateTimeOffset CalculatedAtUtc,
    int ObservationCount,
    IReadOnlyList<MasteryDomainView> Domains,
    IReadOnlyList<MasteryGateView> Gates);
