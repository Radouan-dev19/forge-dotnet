using ForgeDotNet.Domain.Exams;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class ExamAttemptRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ExamId { get; set; } = string.Empty;
    public int ExamVersion { get; set; }
    public string ExamRevision { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal PassingScore { get; set; }
    public string DrawAlgorithm { get; set; } = string.Empty;
    public string DrawSeed { get; set; } = string.Empty;
    public string DrawCommitment { get; set; } = string.Empty;
    public string FrozenItemsJson { get; set; } = string.Empty;
    public ExamAttemptStatus Status { get; set; }
    public int Version { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset DeadlineUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public bool AssistanceDeclared { get; set; }
    public ExamCompletionReason? CompletionReason { get; set; }
    public string? ReportJson { get; set; }
}

internal sealed class ExamSubmissionRecord
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public ExamSubmissionOutcome Outcome { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int HiddenFailureCount { get; set; }
    public Guid DiagnosticId { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; }
}

