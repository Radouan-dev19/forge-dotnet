using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Application.Practice;

public sealed record PracticeExerciseSummaryView(
    string Id,
    int Version,
    string Title,
    int Difficulty,
    int EstimatedMinutes);

public sealed record PracticeReflectionInput(
    string Reformulation,
    string Inputs,
    string ExpectedOutput,
    string EdgeCases,
    string Hypothesis,
    string Plan);

public sealed record PracticeReflectionView(
    string Reformulation,
    string Inputs,
    string ExpectedOutput,
    string EdgeCases,
    string Hypothesis,
    string Plan,
    DateTimeOffset UpdatedAtUtc);

public sealed record PracticeAttemptView(
    int Sequence,
    string SubmissionText,
    string ManualVerificationNotes,
    bool ManualCheckDeclared,
    bool IsSerious,
    string DecisionLabel,
    int? SimilarityWithPreviousPercent,
    string ComparisonSummary,
    DateTimeOffset SubmittedAtUtc);

public sealed record PracticeHintUsageView(
    int Level,
    string KindLabel,
    string Content,
    DateTimeOffset UsedAtUtc);

public sealed record PracticeSolutionEligibilityView(
    bool CanViewSolution,
    int SeriousAttemptCount,
    int RequiredSeriousAttempts,
    DateTimeOffset? AvailableAtUtc,
    int RemainingDelaySeconds,
    string Reason);

public sealed record PracticeActivityView(
    string ExerciseId,
    int ExerciseVersion,
    string ContentRevision,
    string Title,
    int Difficulty,
    int EstimatedMinutes,
    string Statement,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<PracticeExerciseExample> Examples,
    string Starter,
    int Version,
    PracticeActivityState State,
    string StateLabel,
    bool IsManualOnly,
    bool CanEditReflection,
    bool CanSubmitAttempt,
    bool CanUseHint,
    int? NextHintLevel,
    bool CanCompletePostSolutionWork,
    PracticeReflectionView? Reflection,
    IReadOnlyList<PracticeAttemptView> Attempts,
    IReadOnlyList<PracticeHintUsageView> UsedHints,
    PracticeSolutionEligibilityView SolutionEligibility,
    string? Solution,
    string? Explanation,
    string? VariantId,
    string? VariantTitle,
    string? VariantStatement,
    string? PersonalExplanation,
    string? VariantSubmission,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SolutionViewedAtUtc,
    DateTimeOffset? PostSolutionCompletedAtUtc);
