using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Analytics;
using ForgeDotNet.Domain.Analytics;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Domain.WeeklyPlanning;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Analytics;

public sealed class SqliteAnalyticsEvidenceSource(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IAnalyticsEvidenceSource
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public async ValueTask<AnalyticsEvidence> ReadAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var events = new List<AnalyticsActivityEvent>();
        var attempts = new List<AnalyticsAttemptEvidence>();

        LessonReadingActivityRecord[] lessons = await context.LessonReadingActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        foreach (LessonReadingActivityRecord item in lessons)
        {
            events.Add(new($"lesson:{item.LessonId}", item.CompletedAtUtc));
        }

        DiagnosticSessionRecord[] diagnostics = await context.DiagnosticSessions.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        Guid[] diagnosticIds = diagnostics.Select(item => item.Id).ToArray();
        DiagnosticResponseRecord[] responses = await context.DiagnosticResponses.AsNoTracking()
            .Where(item => diagnosticIds.Contains(item.SessionId))
            .ToArrayAsync(cancellationToken);
        foreach (DiagnosticSessionRecord item in diagnostics)
        {
            AddEvents(events, $"diagnostic:{item.Id:N}", item.StartedAtUtc, item.UpdatedAtUtc, item.EndedAtUtc);
        }

        foreach (DiagnosticResponseRecord item in responses)
        {
            events.Add(new($"diagnostic:{item.SessionId:N}", item.SavedAtUtc));
        }

        PracticeActivityRecord[] practiceActivities = await context.PracticeActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        Guid[] practiceIds = practiceActivities.Select(item => item.Id).ToArray();
        PracticeReflectionRecord[] reflections = await context.PracticeReflections.AsNoTracking()
            .Where(item => practiceIds.Contains(item.ActivityId))
            .ToArrayAsync(cancellationToken);
        PracticeHintUsageRecord[] hints = await context.PracticeHintUsages.AsNoTracking()
            .Where(item => practiceIds.Contains(item.ActivityId))
            .ToArrayAsync(cancellationToken);
        PracticeLearningAttemptRecord[] practiceRuns = await context.PracticeLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        foreach (PracticeActivityRecord activity in practiceActivities)
        {
            string key = $"practice:{activity.ExerciseId}";
            AddEvents(events, key, activity.StartedAtUtc, activity.SolutionViewedAtUtc, activity.PostSolutionCompletedAtUtc);
            foreach (PracticeReflectionRecord item in reflections.Where(item => item.ActivityId == activity.Id))
            {
                events.Add(new(key, item.UpdatedAtUtc));
            }

            foreach (PracticeHintUsageRecord item in hints.Where(item => item.ActivityId == activity.Id))
            {
                events.Add(new(key, item.UsedAtUtc));
            }

            PracticeLearningAttemptRecord[] runs = practiceRuns
                .Where(item => string.Equals(item.ExerciseId, activity.ExerciseId, StringComparison.Ordinal))
                .OrderBy(item => item.ObservedAtUtc)
                .ThenBy(item => item.Id)
                .ToArray();
            for (int index = 0; index < runs.Length; index++)
            {
                PracticeLearningAttemptRecord run = runs[index];
                events.Add(new(key, run.ObservedAtUtc));
                attempts.Add(new AnalyticsAttemptEvidence(
                    key,
                    index + 1,
                    run.Status == PracticeLearningAttemptStatus.Succeeded,
                    activity.SolutionViewedAtUtc is not null && activity.SolutionViewedAtUtc <= run.ObservedAtUtc,
                    hints.Where(item => item.ActivityId == activity.Id && item.UsedAtUtc <= run.ObservedAtUtc)
                        .Select(item => item.Level)
                        .DefaultIfEmpty(0)
                        .Max(),
                    run.ObservedAtUtc));
            }
        }

        DebugLabActivityRecord[] debugActivities = await context.DebugLabActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        Guid[] debugIds = debugActivities.Select(item => item.Id).ToArray();
        DebugCorrectionAttemptRecord[] debugAttempts = await context.DebugCorrectionAttempts.AsNoTracking()
            .Where(item => debugIds.Contains(item.ActivityId))
            .ToArrayAsync(cancellationToken);
        foreach (DebugLabActivityRecord activity in debugActivities)
        {
            string key = $"debug:{activity.ScenarioId}";
            AddEvents(events, key, activity.StartedAtUtc, activity.SolutionViewedAtUtc, activity.CompletedAtUtc);
            foreach (DebugCorrectionAttemptRecord item in debugAttempts
                .Where(item => item.ActivityId == activity.Id)
                .OrderBy(item => item.Sequence))
            {
                events.Add(new(key, item.SubmittedAtUtc));
                attempts.Add(new AnalyticsAttemptEvidence(
                    key,
                    item.Sequence,
                    item.Outcome == DebugCorrectionOutcome.Succeeded,
                    activity.SolutionViewedAtUtc is not null && activity.SolutionViewedAtUtc <= item.SubmittedAtUtc,
                    HighestHintLevel: 0,
                    item.SubmittedAtUtc));
            }
        }

        SqlLearningAttemptRecord[] sqlAttempts = await context.SqlLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderBy(item => item.ObservedAtUtc)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        foreach (IGrouping<string, SqlLearningAttemptRecord> group in sqlAttempts.GroupBy(
            item => item.ScenarioId,
            StringComparer.Ordinal))
        {
            int sequence = 0;
            foreach (SqlLearningAttemptRecord item in group)
            {
                sequence++;
                string key = $"sql:{item.ScenarioId}";
                events.Add(new(key, item.ObservedAtUtc));
                attempts.Add(new AnalyticsAttemptEvidence(
                    key,
                    sequence,
                    item.Status == SqlLabExecutionStatus.Succeeded && item.ValidationPassed == true,
                    SolutionViewedBefore: false,
                    HighestHintLevel: 0,
                    item.ObservedAtUtc));
            }
        }

        ReviewItemRecord[] reviewItems = await context.ReviewItems.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        Guid[] reviewIds = reviewItems.Select(item => item.Id).ToArray();
        ReviewAttemptRecord[] reviewAttempts = await context.ReviewAttempts.AsNoTracking()
            .Where(item => reviewIds.Contains(item.ReviewItemId))
            .ToArrayAsync(cancellationToken);
        foreach (ReviewItemRecord item in reviewItems)
        {
            AddEvents(events, $"review:{item.Id:N}", item.CreatedAtUtc, item.LastReviewedAtUtc);
        }

        foreach (ReviewAttemptRecord item in reviewAttempts)
        {
            events.Add(new($"review:{item.ReviewItemId:N}", item.AnsweredAtUtc));
        }

        ExamAttemptRecord[] examAttempts = await context.ExamAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        Guid[] examIds = examAttempts.Select(item => item.Id).ToArray();
        ExamSubmissionRecord[] examSubmissions = await context.ExamSubmissions.AsNoTracking()
            .Where(item => examIds.Contains(item.AttemptId))
            .ToArrayAsync(cancellationToken);
        var exams = new List<AnalyticsExamEvidence>();
        foreach (ExamAttemptRecord exam in examAttempts)
        {
            string contextKey = $"exam:{exam.Id:N}";
            AddEvents(events, contextKey, exam.StartedAtUtc, exam.EndedAtUtc);
            foreach (IGrouping<string, ExamSubmissionRecord> group in examSubmissions
                .Where(item => item.AttemptId == exam.Id)
                .GroupBy(item => item.ItemId, StringComparer.Ordinal))
            {
                foreach (ExamSubmissionRecord item in group.OrderBy(item => item.Sequence))
                {
                    events.Add(new(contextKey, item.SubmittedAtUtc));
                    attempts.Add(new AnalyticsAttemptEvidence(
                        $"exam:{exam.Id:N}:{item.ItemId}",
                        item.Sequence,
                        item.Outcome == ExamSubmissionOutcome.Succeeded,
                        SolutionViewedBefore: false,
                        HighestHintLevel: 0,
                        item.SubmittedAtUtc));
                }
            }

            if (exam.Status != ExamAttemptStatus.Active)
            {
                ExamReport report = DeserializeReport(exam.ReportJson);
                exams.Add(new AnalyticsExamEvidence(
                    exam.Status switch
                    {
                        ExamAttemptStatus.Completed => AnalyticsExamStatus.Completed,
                        ExamAttemptStatus.Abandoned => AnalyticsExamStatus.Abandoned,
                        ExamAttemptStatus.TimedOut => AnalyticsExamStatus.TimedOut,
                        _ => throw new InvalidDataException("Un état d’examen analytique est invalide."),
                    },
                    report.Score,
                    exam.EndedAtUtc ?? throw new InvalidDataException("La fin de l’examen est absente.")));
            }
        }

        WeeklyPlanRecord? acceptedPlan = await context.WeeklyPlans.AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.Status == WeeklyPlanStatus.Accepted)
            .OrderByDescending(item => item.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        string? nextObjective = acceptedPlan is null ? null : ReadObjective(acceptedPlan.PlanJson);
        return new AnalyticsEvidence(
            Array.AsReadOnly(events.ToArray()),
            Array.AsReadOnly(attempts.ToArray()),
            Array.AsReadOnly(exams.ToArray()),
            hints.Length,
            practiceActivities.Count(item => item.SolutionViewedAtUtc is not null)
                + debugActivities.Count(item => item.SolutionViewedAtUtc is not null),
            nextObjective);
    }

    private static void AddEvents(
        List<AnalyticsActivityEvent> events,
        string contextKey,
        params DateTimeOffset?[] instants)
    {
        foreach (DateTimeOffset instant in instants.OfType<DateTimeOffset>())
        {
            events.Add(new AnalyticsActivityEvent(contextKey, instant));
        }
    }

    private static ExamReport DeserializeReport(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? throw new InvalidDataException("Le rapport d’examen analytique est absent.")
            : JsonSerializer.Deserialize<ExamReport>(json, SerializerOptions)
                ?? throw new InvalidDataException("Le rapport d’examen analytique est illisible.");

    private static string? ReadObjective(string json)
    {
        WeeklyPlanSnapshot snapshot = JsonSerializer.Deserialize<WeeklyPlanSnapshot>(json, SerializerOptions)
            ?? throw new InvalidDataException("Le plan accepté est illisible pour le dashboard.");
        WeeklyPlanRules.ValidateSnapshot(snapshot);
        WeeklyPlanWeek? week = snapshot.Weeks.OrderBy(item => item.Number).FirstOrDefault();
        return week is null ? null : $"Semaine {week.Number} — {week.Title}";
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
