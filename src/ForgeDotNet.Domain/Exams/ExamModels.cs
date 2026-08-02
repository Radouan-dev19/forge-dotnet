using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Domain.Exams;

public enum ExamAttemptStatus
{
    Active,
    Completed,
    Abandoned,
    TimedOut,
}

public enum ExamCompletionReason
{
    Submitted,
    Abandoned,
    DeadlineReached,
}

public enum ExamSubmissionOutcome
{
    Succeeded,
    CompilationFailed,
    TestsFailed,
    TimedOut,
    Cancelled,
    Unavailable,
}

public enum ExamSubmissionKind
{
    CSharp,
    Sql,
}

public sealed record ExamCandidate(
    string ItemId,
    int ItemVersion,
    string ContentRevision,
    MasteryDomain Domain,
    string Title,
    string Statement,
    IReadOnlyList<string> Constraints,
    string StarterFileName,
    string StarterCode,
    ExamSubmissionKind SubmissionKind = ExamSubmissionKind.CSharp);

public sealed record ExamBlueprint(
    string Id,
    int Version,
    string Revision,
    string Title,
    int DurationMinutes,
    int DrawCount,
    decimal PassingScore,
    IReadOnlyList<ExamCandidate> Candidates);

public sealed record ExamItemSnapshot(
    int Position,
    string ItemId,
    int ItemVersion,
    string ContentRevision,
    MasteryDomain Domain,
    string Title,
    string Statement,
    IReadOnlyList<string> Constraints,
    string StarterFileName,
    string StarterCode,
    ExamSubmissionKind SubmissionKind = ExamSubmissionKind.CSharp);

public sealed record ExamAttempt(
    Guid Id,
    Guid ProfileId,
    string ExamId,
    int ExamVersion,
    string ExamRevision,
    string Title,
    int DurationMinutes,
    decimal PassingScore,
    string DrawAlgorithm,
    string DrawSeed,
    string DrawCommitment,
    IReadOnlyList<ExamItemSnapshot> Items,
    ExamAttemptStatus Status,
    int Version,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset? EndedAtUtc,
    bool AssistanceDeclared,
    ExamCompletionReason? CompletionReason);

public sealed record ExamSubmission(
    Guid Id,
    Guid AttemptId,
    string ItemId,
    int Sequence,
    string SourceFingerprint,
    string SourceCode,
    ExamSubmissionOutcome Outcome,
    int TotalTests,
    int PassedTests,
    int HiddenFailureCount,
    Guid DiagnosticId,
    DateTimeOffset SubmittedAtUtc);

public sealed record ExamItemReport(
    string ItemId,
    string Title,
    MasteryDomain Domain,
    bool WasSubmitted,
    bool IsAutomaticallyVerified,
    ExamSubmissionOutcome? Outcome,
    decimal Score,
    int TotalTests,
    int PassedTests,
    int HiddenFailureCount,
    int SubmissionCount);

public sealed record ExamReport(
    Guid AttemptId,
    ExamAttemptStatus Status,
    ExamCompletionReason Reason,
    decimal Score,
    bool Passed,
    bool AssistanceDeclared,
    string DrawAlgorithm,
    string DrawSeed,
    string DrawCommitment,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    IReadOnlyList<ExamItemReport> Items);

public sealed record ExamCompletion(ExamAttempt Attempt, ExamReport Report);

