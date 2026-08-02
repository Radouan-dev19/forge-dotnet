using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Exams;

public sealed record ExamSummaryView(
    string Id,
    int Version,
    string Title,
    int DurationMinutes,
    int DrawCount,
    int CandidateCount,
    decimal PassingScore);

public sealed record ExamAttemptSummaryView(
    Guid Id,
    string Title,
    ExamAttemptStatus Status,
    string StatusLabel,
    decimal? Score,
    bool? Passed,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);

public sealed record ExamHomeView(
    IReadOnlyList<ExamSummaryView> Exams,
    ExamAttemptSummaryView? ActiveAttempt,
    IReadOnlyList<ExamAttemptSummaryView> History);

public sealed record ExamItemView(
    int Position,
    string ItemId,
    string Title,
    MasteryDomain Domain,
    string Statement,
    IReadOnlyList<string> Constraints,
    string StarterFileName,
    string SourceCode,
    bool HasSubmission,
    int SubmissionCount,
    DateTimeOffset? LastSubmittedAtUtc);

public sealed record ExamItemReportView(
    string ItemId,
    string Title,
    string DomainLabel,
    bool WasSubmitted,
    bool IsAutomaticallyVerified,
    string OutcomeLabel,
    decimal Score,
    int TotalTests,
    int PassedTests,
    int HiddenFailureCount,
    int SubmissionCount);

public sealed record ExamReportView(
    ExamAttemptStatus Status,
    string StatusLabel,
    decimal Score,
    bool Passed,
    bool AssistanceDeclared,
    string DrawAlgorithm,
    string DrawSeed,
    string DrawCommitment,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    IReadOnlyList<ExamItemReportView> Items);

public sealed record ExamAttemptView(
    Guid Id,
    int Version,
    string Title,
    ExamAttemptStatus Status,
    string StatusLabel,
    int DurationMinutes,
    int RemainingSeconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset DeadlineUtc,
    string DrawAlgorithm,
    string DrawCommitment,
    bool CanResume,
    bool CanSubmit,
    IReadOnlyList<ExamItemView> Items,
    ExamReportView? Report);

public sealed record ExamSubmissionReceiptView(
    Guid AttemptId,
    int AttemptVersion,
    string ItemId,
    int Sequence,
    DateTimeOffset SubmittedAtUtc,
    string Message);

