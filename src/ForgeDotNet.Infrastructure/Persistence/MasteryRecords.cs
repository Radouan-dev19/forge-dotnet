using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.Projects;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class PracticeLearningAttemptRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ExerciseId { get; set; } = string.Empty;
    public int ExerciseVersion { get; set; }
    public string ContentRevision { get; set; } = string.Empty;
    public string SubmissionFingerprint { get; set; } = string.Empty;
    public PracticeLearningAttemptStatus Status { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public Guid DiagnosticId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
}

/// <summary>
/// Soumission de projet, ajoutée sans jamais être modifiée, comme les observations de pratique.
/// </summary>
internal sealed class ProjectSubmissionRecord
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    public int ProjectVersion { get; set; }

    public string ContentRevision { get; set; } = string.Empty;

    public string SubmissionFingerprint { get; set; } = string.Empty;

    public ProjectSubmissionStatus Status { get; set; }

    public int TotalSuites { get; set; }

    public int PassedSuites { get; set; }

    public int TotalTests { get; set; }

    public int PassedTests { get; set; }

    public bool AutomaticallyVerified { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }
}

internal sealed class SqlLearningAttemptRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public int ScenarioVersion { get; set; }
    public string ContentRevision { get; set; } = string.Empty;
    public SqlLabExecutionStatus Status { get; set; }
    public bool ValidationRequested { get; set; }
    public bool? ValidationPassed { get; set; }
    public string QueryFingerprint { get; set; } = string.Empty;
    public Guid DiagnosticId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public long ElapsedMilliseconds { get; set; }
}

internal sealed class MasteryProjectionRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public int PolicyVersion { get; set; }
    public string PolicyRevision { get; set; } = string.Empty;
    public string EvidenceRevision { get; set; } = string.Empty;
    public string FrozenPolicyJson { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
