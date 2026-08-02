using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class DiagnosticSessionRecord
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public required string BankId { get; set; }

    public int BankVersion { get; set; }

    public required string BankRevision { get; set; }

    public DiagnosticMode Mode { get; set; }

    public int Seed { get; set; }

    public DiagnosticSessionStatus Status { get; set; }

    public int CurrentSectionIndex { get; set; }

    public required string SectionStatusesJson { get; set; }

    public required string FrozenPlanJson { get; set; }

    public int SectionDurationSeconds { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }

    public DateTimeOffset? SectionStartedAtUtc { get; set; }

    public DateTimeOffset? SectionDeadlineUtc { get; set; }
}
