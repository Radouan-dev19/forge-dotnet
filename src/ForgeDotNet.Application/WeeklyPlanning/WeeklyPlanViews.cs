using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.Application.WeeklyPlanning;

public sealed record WeeklyPlanRecommendationView(
    string DomainId,
    string DisplayName,
    bool IsCritical,
    WeeklyPlanRecommendationKind Kind,
    string KindLabel,
    int Priority,
    decimal DiagnosticScore,
    string Rationale);

public sealed record WeeklyPlanWeekFocusView(
    string DomainId,
    string DisplayName,
    WeeklyPlanDepth Depth,
    string DepthLabel);

public sealed record WeeklyPlanWeekView(
    string CurriculumWeekId,
    int Number,
    string Title,
    IReadOnlyList<string> Prerequisites,
    int PlannedHours,
    decimal CoreLearningHours,
    decimal RemediationHours,
    decimal ConsolidationHours,
    decimal KnowledgeCheckHours,
    bool KnowledgeCheckRequired,
    IReadOnlyList<WeeklyPlanWeekFocusView> Focuses,
    string Explanation);

public sealed record WeeklyPlanView(
    Guid Id,
    Guid DiagnosticSessionId,
    int Version,
    WeeklyPlanStatus Status,
    string StatusLabel,
    string CurriculumId,
    int CurriculumVersion,
    string CurriculumRevision,
    int ProfileAvailableHours,
    int CurrentProfileAvailableHours,
    int TargetWeeklyHours,
    bool IsProvisional,
    bool CanAdjust,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<WeeklyPlanRecommendationView> Recommendations,
    IReadOnlyList<WeeklyPlanWeekView> Weeks,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? AcceptedAtUtc);
