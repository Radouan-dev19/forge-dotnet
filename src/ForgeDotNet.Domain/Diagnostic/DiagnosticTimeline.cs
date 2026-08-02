namespace ForgeDotNet.Domain.Diagnostic;

public enum DiagnosticSessionStatus
{
    Active,
    Completed,
    Abandoned,
}

public enum DiagnosticSectionStatus
{
    Pending,
    Active,
    Completed,
    Expired,
    Interrupted,
}

public sealed record DiagnosticTimeline(
    DiagnosticSessionStatus SessionStatus,
    int CurrentSectionIndex,
    IReadOnlyList<DiagnosticSectionStatus> SectionStatuses,
    DateTimeOffset? SectionStartedAtUtc,
    DateTimeOffset? SectionDeadlineUtc);

public static class DiagnosticTimelineRules
{
    public static DiagnosticTimeline CreateStarted(
        int sectionCount,
        DateTimeOffset now,
        TimeSpan sectionDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sectionCount, 1);
        ValidateDuration(sectionDuration);
        DiagnosticSectionStatus[] statuses = Enumerable
            .Repeat(DiagnosticSectionStatus.Pending, sectionCount)
            .ToArray();
        statuses[0] = DiagnosticSectionStatus.Active;
        return new DiagnosticTimeline(
            DiagnosticSessionStatus.Active,
            0,
            Array.AsReadOnly(statuses),
            now,
            now.Add(sectionDuration));
    }

    public static DiagnosticTimeline Refresh(DiagnosticTimeline timeline, DateTimeOffset now)
    {
        Validate(timeline);
        if (timeline.SessionStatus != DiagnosticSessionStatus.Active
            || timeline.CurrentSectionIndex >= timeline.SectionStatuses.Count
            || timeline.SectionStatuses[timeline.CurrentSectionIndex] != DiagnosticSectionStatus.Active
            || timeline.SectionDeadlineUtc is null
            || now < timeline.SectionDeadlineUtc.Value)
        {
            return timeline;
        }

        DiagnosticSectionStatus[] statuses = timeline.SectionStatuses.ToArray();
        statuses[timeline.CurrentSectionIndex] = DiagnosticSectionStatus.Expired;
        return new DiagnosticTimeline(
            timeline.SessionStatus,
            timeline.CurrentSectionIndex + 1,
            Array.AsReadOnly(statuses),
            null,
            null);
    }

    public static DiagnosticTimeline StartCurrent(
        DiagnosticTimeline timeline,
        DateTimeOffset now,
        TimeSpan sectionDuration)
    {
        DiagnosticTimeline refreshed = Refresh(timeline, now);
        ValidateDuration(sectionDuration);
        if (refreshed.SessionStatus != DiagnosticSessionStatus.Active
            || refreshed.CurrentSectionIndex >= refreshed.SectionStatuses.Count
            || refreshed.SectionStatuses[refreshed.CurrentSectionIndex] != DiagnosticSectionStatus.Pending)
        {
            throw new InvalidOperationException("Aucune section en attente ne peut être démarrée.");
        }

        DiagnosticSectionStatus[] statuses = refreshed.SectionStatuses.ToArray();
        statuses[refreshed.CurrentSectionIndex] = DiagnosticSectionStatus.Active;
        return refreshed with
        {
            SectionStatuses = Array.AsReadOnly(statuses),
            SectionStartedAtUtc = now,
            SectionDeadlineUtc = now.Add(sectionDuration),
        };
    }

    public static DiagnosticTimeline CompleteCurrent(
        DiagnosticTimeline timeline,
        int sectionIndex,
        DateTimeOffset now)
    {
        DiagnosticTimeline refreshed = Refresh(timeline, now);
        if (sectionIndex >= 0
            && sectionIndex < refreshed.SectionStatuses.Count
            && refreshed.SectionStatuses[sectionIndex] is DiagnosticSectionStatus.Completed
                or DiagnosticSectionStatus.Expired)
        {
            return refreshed;
        }

        if (refreshed.SessionStatus != DiagnosticSessionStatus.Active
            || sectionIndex != refreshed.CurrentSectionIndex
            || refreshed.CurrentSectionIndex >= refreshed.SectionStatuses.Count
            || refreshed.SectionStatuses[sectionIndex] != DiagnosticSectionStatus.Active)
        {
            throw new InvalidOperationException("La section n'est pas active.");
        }

        DiagnosticSectionStatus[] statuses = refreshed.SectionStatuses.ToArray();
        statuses[sectionIndex] = DiagnosticSectionStatus.Completed;
        return refreshed with
        {
            CurrentSectionIndex = sectionIndex + 1,
            SectionStatuses = Array.AsReadOnly(statuses),
            SectionStartedAtUtc = null,
            SectionDeadlineUtc = null,
        };
    }

    public static DiagnosticTimeline Finish(DiagnosticTimeline timeline, DateTimeOffset now)
    {
        DiagnosticTimeline refreshed = Refresh(timeline, now);
        if (refreshed.SessionStatus == DiagnosticSessionStatus.Completed)
        {
            return refreshed;
        }

        if (refreshed.SessionStatus != DiagnosticSessionStatus.Active
            || refreshed.SectionStatuses.Any(status => status is DiagnosticSectionStatus.Pending or DiagnosticSectionStatus.Active))
        {
            throw new InvalidOperationException("Toutes les sections doivent être terminées ou expirées.");
        }

        return refreshed with { SessionStatus = DiagnosticSessionStatus.Completed };
    }

    public static DiagnosticTimeline Abandon(DiagnosticTimeline timeline)
    {
        Validate(timeline);
        if (timeline.SessionStatus == DiagnosticSessionStatus.Abandoned)
        {
            return timeline;
        }

        if (timeline.SessionStatus != DiagnosticSessionStatus.Active)
        {
            throw new InvalidOperationException("Une session terminée ne peut pas être abandonnée.");
        }

        DiagnosticSectionStatus[] statuses = timeline.SectionStatuses.ToArray();
        if (timeline.CurrentSectionIndex < statuses.Length
            && statuses[timeline.CurrentSectionIndex] == DiagnosticSectionStatus.Active)
        {
            statuses[timeline.CurrentSectionIndex] = DiagnosticSectionStatus.Interrupted;
        }

        return timeline with
        {
            SessionStatus = DiagnosticSessionStatus.Abandoned,
            SectionStatuses = Array.AsReadOnly(statuses),
            SectionStartedAtUtc = null,
            SectionDeadlineUtc = null,
        };
    }

    public static bool CanAnswer(
        DiagnosticTimeline timeline,
        int sectionIndex,
        DateTimeOffset now)
    {
        DiagnosticTimeline refreshed = Refresh(timeline, now);
        return refreshed.SessionStatus == DiagnosticSessionStatus.Active
            && refreshed.CurrentSectionIndex == sectionIndex
            && sectionIndex < refreshed.SectionStatuses.Count
            && refreshed.SectionStatuses[sectionIndex] == DiagnosticSectionStatus.Active
            && refreshed.SectionDeadlineUtc is not null
            && now < refreshed.SectionDeadlineUtc.Value;
    }

    public static TimeSpan GetRemaining(DiagnosticTimeline timeline, DateTimeOffset now)
    {
        DiagnosticTimeline refreshed = Refresh(timeline, now);
        if (refreshed.SectionDeadlineUtc is null)
        {
            return TimeSpan.Zero;
        }

        TimeSpan remaining = refreshed.SectionDeadlineUtc.Value - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private static void Validate(DiagnosticTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        if (timeline.SectionStatuses.Count == 0
            || timeline.CurrentSectionIndex < 0
            || timeline.CurrentSectionIndex > timeline.SectionStatuses.Count)
        {
            throw new ArgumentException("La chronologie de diagnostic est invalide.", nameof(timeline));
        }
    }

    private static void ValidateDuration(TimeSpan sectionDuration)
    {
        if (sectionDuration <= TimeSpan.Zero || sectionDuration > TimeSpan.FromHours(2))
        {
            throw new ArgumentOutOfRangeException(nameof(sectionDuration));
        }
    }
}
