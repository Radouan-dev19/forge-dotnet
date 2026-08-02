using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class WeeklyPlanRecord
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public Guid DiagnosticSessionId { get; set; }

    public int Version { get; set; }

    public WeeklyPlanStatus Status { get; set; }

    public required string CurriculumId { get; set; }

    public int CurriculumVersion { get; set; }

    public required string CurriculumRevision { get; set; }

    public int TargetWeeklyHours { get; set; }

    public required string PlanJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? AcceptedAtUtc { get; set; }
}
