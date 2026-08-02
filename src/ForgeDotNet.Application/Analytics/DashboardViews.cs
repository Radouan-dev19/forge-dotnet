using ForgeDotNet.Application.Mastery;

namespace ForgeDotNet.Application.Analytics;

public sealed record DashboardDomainView(string Label, decimal Score, decimal RequiredScore);

public sealed record LearningDashboardView(
    int InactivityThresholdMinutes,
    int? ActiveMinutes,
    int ActiveIntervalCount,
    decimal? FirstAttemptSuccessRate,
    decimal? BeforeSolutionSuccessRate,
    int AttemptCount,
    int HintUsageCount,
    int SolutionViewCount,
    int DueReviewCount,
    DateOnly? NextReviewDate,
    string? NextObjective,
    int CompletedExamCount,
    int AbandonedExamCount,
    int TimedOutExamCount,
    decimal? AverageExamScore,
    IReadOnlyList<DashboardDomainView> Strengths,
    IReadOnlyList<DashboardDomainView> Weaknesses,
    IReadOnlyList<MasteryGateView> Gates,
    string MeasurementNote);

