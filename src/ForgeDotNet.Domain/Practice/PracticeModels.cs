namespace ForgeDotNet.Domain.Practice;

public enum PracticeActivityState
{
    ReflectionRequired,
    Attempting,
    SolutionViewed,
    PostSolutionCompleted,
}

public enum PracticeAttemptDecision
{
    Serious,
    SubmissionTooShort,
    ManualCheckMissing,
    VerificationNotesTooShort,
    SubstantialDuplicate,
}

public sealed record PracticeReflection(
    string Reformulation,
    string Inputs,
    string ExpectedOutput,
    string EdgeCases,
    string Hypothesis,
    string Plan,
    DateTimeOffset UpdatedAtUtc);

public sealed record PracticeAttempt(
    Guid Id,
    int Sequence,
    string SubmissionText,
    string ManualVerificationNotes,
    bool ManualCheckDeclared,
    bool IsSerious,
    PracticeAttemptDecision Decision,
    string SubmissionFingerprint,
    DateTimeOffset SubmittedAtUtc);

public sealed record PracticeHintUsage(
    Guid Id,
    int Level,
    string Kind,
    DateTimeOffset UsedAtUtc);

public sealed record PracticeHint(int Level, string Kind, string Content);

public sealed record PracticeExerciseExample(string Input, string Output);

public sealed record PracticeExercise(
    string Id,
    int Version,
    string Revision,
    string Title,
    int Difficulty,
    int EstimatedMinutes,
    // La première compétence décide du domaine de maîtrise alimenté par cet exercice.
    IReadOnlyList<string> Skills,
    string Statement,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<PracticeExerciseExample> Examples,
    string Starter,
    IReadOnlyList<PracticeHint> Hints,
    int RequiredSeriousAttempts,
    TimeSpan MinimumSolutionDelay,
    string Solution,
    string Explanation,
    string VariantId,
    string VariantTitle,
    string VariantStatement);

public sealed record PracticeActivity(
    Guid Id,
    Guid ProfileId,
    string ExerciseId,
    int ExerciseVersion,
    string ContentRevision,
    int Version,
    PracticeActivityState State,
    DateTimeOffset StartedAtUtc,
    PracticeReflection? Reflection,
    IReadOnlyList<PracticeAttempt> Attempts,
    IReadOnlyList<PracticeHintUsage> HintUsages,
    DateTimeOffset? SolutionViewedAtUtc,
    string? PersonalExplanation,
    string? VariantSubmission,
    DateTimeOffset? PostSolutionCompletedAtUtc);

public sealed record PracticeAttemptInput(
    string SubmissionText,
    string ManualVerificationNotes,
    bool ManualCheckDeclared);

public sealed record PracticeSolutionEligibility(
    bool CanViewSolution,
    int SeriousAttemptCount,
    int RequiredSeriousAttempts,
    DateTimeOffset? AvailableAtUtc,
    TimeSpan RemainingDelay,
    string Reason);

public sealed record PracticeTextComparison(
    int SimilarityPercent,
    bool IsSubstantialDuplicate,
    string Summary);
