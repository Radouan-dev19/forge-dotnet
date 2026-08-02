using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class ReviewItemRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public ReviewSourceKind SourceKind { get; set; }
    public string SourceItemId { get; set; } = string.Empty;
    public int SourceItemVersion { get; set; }
    public string SourceRevision { get; set; } = string.Empty;
    public DateTimeOffset SourceOccurredAtUtc { get; set; }
    public MasteryDomain Domain { get; set; }
    public ReviewScheduleKind ScheduleKind { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? ExpectedAnswer { get; set; }
    public string ChoicesJson { get; set; } = "[]";
    public ReviewEvaluationMode EvaluationMode { get; set; }
    public bool CanProduceMasteryEvidence { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public int PolicyVersion { get; set; }
    public string PolicyRevision { get; set; } = string.Empty;
    public int CurrentIntervalIndex { get; set; }
    public DateOnly DueOn { get; set; }
    public int AttemptCount { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastReviewedAtUtc { get; set; }
}

internal sealed class ReviewAttemptRecord
{
    public Guid Id { get; set; }
    public Guid ReviewItemId { get; set; }
    public int Sequence { get; set; }
    public ReviewOutcome Outcome { get; set; }
    public bool IsVerified { get; set; }
    public bool IsMasteryEligible { get; set; }
    public decimal Score { get; set; }
    public string ResponseFingerprint { get; set; } = string.Empty;
    public DateOnly PreviousDueOn { get; set; }
    public DateOnly NextDueOn { get; set; }
    public int NextIntervalDays { get; set; }
    public DateTimeOffset AnsweredAtUtc { get; set; }
}
