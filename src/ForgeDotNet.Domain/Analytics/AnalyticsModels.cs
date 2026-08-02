namespace ForgeDotNet.Domain.Analytics;

public enum AnalyticsExamStatus
{
    Completed,
    Abandoned,
    TimedOut,
}

public sealed record AnalyticsActivityEvent(string ContextKey, DateTimeOffset OccurredAtUtc);

public sealed record AnalyticsAttemptEvidence(
    string ActivityKey,
    int Sequence,
    bool Passed,
    bool SolutionViewedBefore,
    int HighestHintLevel,
    DateTimeOffset ObservedAtUtc);

public sealed record AnalyticsExamEvidence(AnalyticsExamStatus Status, decimal Score, DateTimeOffset EndedAtUtc);

public sealed record AnalyticsEvidence(
    IReadOnlyList<AnalyticsActivityEvent> ActivityEvents,
    IReadOnlyList<AnalyticsAttemptEvidence> Attempts,
    IReadOnlyList<AnalyticsExamEvidence> Exams,
    int HintUsageCount,
    int SolutionViewCount,
    string? NextObjective);

public sealed record AnalyticsSnapshot(
    int InactivityThresholdMinutes,
    int ObservedActiveMinutes,
    int ActiveIntervalCount,
    int AttemptCount,
    int FirstAttemptCount,
    int FirstAttemptSuccessCount,
    int BeforeSolutionSuccessCount,
    decimal? FirstAttemptSuccessRate,
    decimal? BeforeSolutionSuccessRate,
    int HintUsageCount,
    int SolutionViewCount,
    int CompletedExamCount,
    int AbandonedExamCount,
    int TimedOutExamCount,
    decimal? AverageExamScore,
    string? NextObjective);

