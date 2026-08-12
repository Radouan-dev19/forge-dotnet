using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Domain.Reviews;

public enum ReviewSourceKind
{
    PracticeError,
    DebuggingBug,
    SqlError,
    ExamFailure,
    MissedDiagnosticQuestion,
    SolutionViewed,
    Personal,

    /// <summary>
    /// Carte de révision attachée à un exercice déjà pratiqué, corrigée côté serveur.
    /// </summary>
    ExerciseReviewCard,
}

public enum ReviewScheduleKind
{
    General,
    Recovery,
}

public enum ReviewEvaluationMode
{
    SelfAssessment,
    Choice,
    ExactText,
}

public enum ReviewOutcome
{
    Succeeded,
    Failed,
}

public sealed record ReviewChoice(string Id, string Text);

public sealed record ReviewSource(
    string Key,
    ReviewSourceKind Kind,
    string ItemId,
    int ItemVersion,
    string Revision,
    DateTimeOffset OccurredAtUtc);

public sealed record ReviewCard(
    string Question,
    string? ExpectedAnswer,
    IReadOnlyList<ReviewChoice> Choices,
    ReviewEvaluationMode EvaluationMode,
    bool CanProduceMasteryEvidence);

public sealed record ReviewPolicy(
    string Id,
    int Version,
    string Revision,
    string TimeZoneId,
    IReadOnlyList<int> GeneralIntervalsDays,
    IReadOnlyList<int> RecoveryIntervalsDays);

public sealed record ReviewItem(
    Guid Id,
    Guid ProfileId,
    ReviewSource Source,
    MasteryDomain Domain,
    ReviewScheduleKind ScheduleKind,
    ReviewCard Card,
    string PolicyId,
    int PolicyVersion,
    string PolicyRevision,
    int CurrentIntervalIndex,
    DateOnly DueOn,
    int AttemptCount,
    int Version,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastReviewedAtUtc);

public sealed record ReviewAnswer(
    string Response,
    bool? SelfReportedSuccess);

public sealed record ReviewAttempt(
    Guid Id,
    Guid ReviewItemId,
    int Sequence,
    ReviewOutcome Outcome,
    bool IsVerified,
    bool IsMasteryEligible,
    decimal Score,
    string ResponseFingerprint,
    DateOnly PreviousDueOn,
    DateOnly NextDueOn,
    int NextIntervalDays,
    DateTimeOffset AnsweredAtUtc);

public sealed record ReviewTransition(ReviewItem Item, ReviewAttempt Attempt);
