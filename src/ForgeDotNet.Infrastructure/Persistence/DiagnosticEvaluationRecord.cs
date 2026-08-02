namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class DiagnosticEvaluationRecord
{
    public Guid SessionId { get; set; }

    public required string RubricId { get; set; }

    public int RubricVersion { get; set; }

    public required string RubricRevision { get; set; }

    public required string FrozenRubricJson { get; set; }

    public required string ReportJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
