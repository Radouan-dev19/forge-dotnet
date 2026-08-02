using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.Application.Reviews;

public sealed record ReviewChoiceView(string Id, string Text);

public sealed record ReviewItemView(
    Guid Id,
    int Version,
    string SourceLabel,
    string ItemId,
    string DomainLabel,
    string Question,
    IReadOnlyList<ReviewChoiceView> Choices,
    ReviewEvaluationMode EvaluationMode,
    DateOnly DueOn,
    bool IsDue,
    int DaysAvailable,
    int CurrentIntervalDays,
    int AttemptCount,
    string EvaluationExplanation);

public sealed record ReviewQueueView(
    string PolicyId,
    int PolicyVersion,
    string PolicyRevision,
    string TimeZoneId,
    DateOnly Today,
    IReadOnlyList<ReviewItemView> DueItems,
    IReadOnlyList<ReviewItemView> UpcomingItems);

public sealed record ReviewAnswerInput(
    string Response,
    bool? SelfReportedSuccess);

public sealed record ReviewAnswerResultView(
    ReviewOutcome Outcome,
    bool IsVerified,
    bool IsMasteryEligible,
    string? ExpectedAnswer,
    DateOnly NextDueOn,
    int NextIntervalDays,
    string Explanation);

public sealed record PersonalReviewCardInput(
    MasteryDomain Domain,
    string Question,
    string ExpectedAnswer);
