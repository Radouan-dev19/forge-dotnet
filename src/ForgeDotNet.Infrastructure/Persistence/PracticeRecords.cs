using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class PracticeActivityRecord
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string ExerciseId { get; set; } = string.Empty;

    public int ExerciseVersion { get; set; }

    public string ContentRevision { get; set; } = string.Empty;

    public int Version { get; set; }

    public PracticeActivityState State { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? SolutionViewedAtUtc { get; set; }

    public string? PersonalExplanation { get; set; }

    public string? VariantSubmission { get; set; }

    public DateTimeOffset? PostSolutionCompletedAtUtc { get; set; }
}

internal sealed class PracticeReflectionRecord
{
    public Guid ActivityId { get; set; }

    public string Reformulation { get; set; } = string.Empty;

    public string Inputs { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;

    public string EdgeCases { get; set; } = string.Empty;

    public string Hypothesis { get; set; } = string.Empty;

    public string Plan { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class PracticeAttemptRecord
{
    public Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public int Sequence { get; set; }

    public string SubmissionText { get; set; } = string.Empty;

    public string ManualVerificationNotes { get; set; } = string.Empty;

    public bool ManualCheckDeclared { get; set; }

    public bool IsSerious { get; set; }

    public PracticeAttemptDecision Decision { get; set; }

    public string SubmissionFingerprint { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAtUtc { get; set; }
}

internal sealed class PracticeHintUsageRecord
{
    public Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public int Level { get; set; }

    public string Kind { get; set; } = string.Empty;

    public DateTimeOffset UsedAtUtc { get; set; }
}
