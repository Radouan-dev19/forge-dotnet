using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class DebugLabActivityRecord
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public int ScenarioVersion { get; set; }
    public string ContentRevision { get; set; } = string.Empty;
    public int Version { get; set; }
    public DebugLabState State { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public string Symptom { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Hypotheses { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string Cause { get; set; } = string.Empty;
    public string Fix { get; set; } = string.Empty;
    public string Test { get; set; } = string.Empty;
    public string Prevention { get; set; } = string.Empty;
    public string? Breakpoint { get; set; }
    public string? Watch { get; set; }
    public string? Locals { get; set; }
    public string? CallStack { get; set; }
    public string? EvaluationJson { get; set; }
    public DateTimeOffset? SolutionViewedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

internal sealed class DebugCorrectionAttemptRecord
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public int Sequence { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
    public DebugCorrectionOutcome Outcome { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public Guid DiagnosticId { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; }
}
