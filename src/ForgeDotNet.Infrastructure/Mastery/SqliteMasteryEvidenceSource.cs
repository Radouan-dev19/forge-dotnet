using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Mastery;

public sealed class SqliteMasteryEvidenceSource(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IMasteryEvidenceSource
{
    private static readonly JsonSerializerOptions RevisionJsonOptions = CreateSerializerOptions();

    public async ValueTask<MasteryEvidenceSet> ReadAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("Le profil de maîtrise est obligatoire.", nameof(profileId));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var observations = new List<MasteryObservation>();
        await AddPracticeAsync(context, profileId, observations, cancellationToken);
        await AddDebugAsync(context, profileId, observations, cancellationToken);
        await AddSqlAsync(context, profileId, observations, cancellationToken);
        await AddReviewsAsync(context, profileId, observations, cancellationToken);
        var achievements = new List<MasteryAchievement>();
        await AddExamsAsync(context, profileId, observations, achievements, cancellationToken);
        MasteryObservation[] ordered = observations.OrderBy(item => item.Id).ToArray();
        MasteryAchievement[] orderedAchievements = achievements.OrderBy(item => item.Id).ToArray();
        string json = JsonSerializer.Serialize(new { ordered, orderedAchievements }, RevisionJsonOptions);
        string revision = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}";
        return new MasteryEvidenceSet(
            Array.AsReadOnly(ordered),
            Array.AsReadOnly(orderedAchievements),
            revision);
    }

    private static async Task AddPracticeAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        CancellationToken cancellationToken)
    {
        PracticeActivityRecord[] activities = await context.PracticeActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        PracticeLearningAttemptRecord[] runnerAttempts = await context.PracticeLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        if (activities.Length == 0 && runnerAttempts.Length == 0)
        {
            return;
        }

        if (activities.Length == 0)
        {
            throw new InvalidDataException("Une observation C# ne correspond à aucune activité de pratique.");
        }

        Guid[] activityIds = activities.Select(item => item.Id).ToArray();
        PracticeAttemptRecord[] attempts = await context.PracticeAttempts.AsNoTracking()
            .Where(item => activityIds.Contains(item.ActivityId))
            .ToArrayAsync(cancellationToken);
        PracticeHintUsageRecord[] hints = await context.PracticeHintUsages.AsNoTracking()
            .Where(item => activityIds.Contains(item.ActivityId))
            .ToArrayAsync(cancellationToken);
        foreach (PracticeAttemptRecord attempt in attempts)
        {
            PracticeActivityRecord activity = activities.Single(item => item.Id == attempt.ActivityId);
            int highestHint = hints
                .Where(item => item.ActivityId == activity.Id && item.UsedAtUtc <= attempt.SubmittedAtUtc)
                .Select(item => item.Level)
                .DefaultIfEmpty(0)
                .Max();
            MasteryAssistance assistance = activity.SolutionViewedAtUtc is not null
                ? MasteryAssistance.Solution
                : HintAssistance(highestHint);
            observations.Add(new MasteryObservation(
                attempt.Id,
                profileId,
                MasteryDomain.CSharp,
                MasteryComponent.AutonomousPractice,
                MasteryEvidenceSource.Practice,
                MasteryVerificationKind.ManualDeclaration,
                activity.ExerciseId,
                activity.ExerciseVersion,
                activity.ContentRevision,
                attempt.IsSerious ? 100m : 0m,
                assistance,
                attempt.SubmittedAtUtc,
                $"practice:{attempt.Id:N}"));
        }

        foreach (PracticeLearningAttemptRecord attempt in runnerAttempts)
        {
            PracticeActivityRecord activity = activities.SingleOrDefault(item =>
                string.Equals(item.ExerciseId, attempt.ExerciseId, StringComparison.Ordinal))
                ?? throw new InvalidDataException("Une observation C# ne correspond à aucune activité de pratique.");
            if (activity.ExerciseVersion != attempt.ExerciseVersion
                || !string.Equals(activity.ContentRevision, attempt.ContentRevision, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Une observation C# ne correspond pas à la version de pratique figée.");
            }

            int highestHint = hints
                .Where(item => item.ActivityId == activity.Id && item.UsedAtUtc <= attempt.ObservedAtUtc)
                .Select(item => item.Level)
                .DefaultIfEmpty(0)
                .Max();
            MasteryAssistance assistance = activity.SolutionViewedAtUtc is not null
                ? MasteryAssistance.Solution
                : HintAssistance(highestHint);
            bool testsObserved = attempt.TotalTests > 0
                && attempt.Status is PracticeLearningAttemptStatus.Succeeded
                    or PracticeLearningAttemptStatus.TestsFailed;
            decimal score = testsObserved
                ? Math.Round(100m * attempt.PassedTests / attempt.TotalTests, 2, MidpointRounding.AwayFromZero)
                : 0m;
            observations.Add(new MasteryObservation(
                attempt.Id,
                profileId,
                MasteryDomain.CSharp,
                MasteryComponent.AutonomousPractice,
                MasteryEvidenceSource.Practice,
                testsObserved ? MasteryVerificationKind.AutomaticTests : MasteryVerificationKind.ManualDeclaration,
                attempt.ExerciseId,
                attempt.ExerciseVersion,
                attempt.ContentRevision,
                score,
                assistance,
                attempt.ObservedAtUtc,
                $"practice-run:{attempt.Id:N}"));
        }
    }

    private static async Task AddDebugAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        CancellationToken cancellationToken)
    {
        DebugLabActivityRecord[] activities = await context.DebugLabActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        if (activities.Length == 0)
        {
            return;
        }

        Guid[] activityIds = activities.Select(item => item.Id).ToArray();
        DebugCorrectionAttemptRecord[] attempts = await context.DebugCorrectionAttempts.AsNoTracking()
            .Where(item => activityIds.Contains(item.ActivityId))
            .ToArrayAsync(cancellationToken);
        foreach (DebugCorrectionAttemptRecord attempt in attempts)
        {
            DebugLabActivityRecord activity = activities.Single(item => item.Id == attempt.ActivityId);
            decimal score = attempt.TotalTests <= 0
                ? 0m
                : Math.Round(100m * attempt.PassedTests / attempt.TotalTests, 2, MidpointRounding.AwayFromZero);
            observations.Add(new MasteryObservation(
                attempt.Id,
                profileId,
                MasteryDomain.Debugging,
                MasteryComponent.AutonomousPractice,
                MasteryEvidenceSource.DebugLab,
                MasteryVerificationKind.AutomaticTests,
                activity.ScenarioId,
                activity.ScenarioVersion,
                activity.ContentRevision,
                score,
                activity.SolutionViewedAtUtc is null ? MasteryAssistance.None : MasteryAssistance.Solution,
                attempt.SubmittedAtUtc,
                $"debug:{attempt.Id:N}"));
        }
    }

    private static async Task AddSqlAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        CancellationToken cancellationToken)
    {
        SqlLearningAttemptRecord[] attempts = await context.SqlLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        foreach (SqlLearningAttemptRecord attempt in attempts)
        {
            bool verified = attempt.ValidationRequested && attempt.ValidationPassed == true;
            observations.Add(new MasteryObservation(
                attempt.Id,
                profileId,
                MasteryDomain.Sql,
                MasteryComponent.AutonomousPractice,
                MasteryEvidenceSource.SqlLab,
                verified ? MasteryVerificationKind.AutomaticTests : MasteryVerificationKind.ManualDeclaration,
                attempt.ScenarioId,
                attempt.ScenarioVersion,
                attempt.ContentRevision,
                verified ? 100m : 0m,
                MasteryAssistance.None,
                attempt.ObservedAtUtc,
                $"sql:{attempt.Id:N}"));
        }
    }

    private static async Task AddReviewsAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        CancellationToken cancellationToken)
    {
        ReviewItemRecord[] items = await context.ReviewItems.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        if (items.Length == 0)
        {
            return;
        }

        Guid[] itemIds = items.Select(item => item.Id).ToArray();
        ReviewAttemptRecord[] attempts = await context.ReviewAttempts.AsNoTracking()
            .Where(item => itemIds.Contains(item.ReviewItemId) && item.IsMasteryEligible)
            .ToArrayAsync(cancellationToken);
        foreach (ReviewAttemptRecord attempt in attempts)
        {
            ReviewItemRecord item = items.Single(candidate => candidate.Id == attempt.ReviewItemId);
            observations.Add(new MasteryObservation(
                attempt.Id,
                profileId,
                item.Domain,
                MasteryComponent.SpacedRetention,
                MasteryEvidenceSource.Review,
                MasteryVerificationKind.ReviewEngine,
                item.SourceItemId,
                item.SourceItemVersion,
                item.SourceRevision,
                attempt.Score,
                MasteryAssistance.None,
                attempt.AnsweredAtUtc,
                $"review:{attempt.Id:N}"));
        }
    }

    private static async Task AddExamsAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        List<MasteryAchievement> achievements,
        CancellationToken cancellationToken)
    {
        ExamAttemptRecord[] attempts = await context.ExamAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.Status != ExamAttemptStatus.Active)
            .ToArrayAsync(cancellationToken);
        if (attempts.Length == 0)
        {
            return;
        }

        Guid[] attemptIds = attempts.Select(item => item.Id).ToArray();
        ExamSubmissionRecord[] submissions = await context.ExamSubmissions.AsNoTracking()
            .Where(item => attemptIds.Contains(item.AttemptId))
            .ToArrayAsync(cancellationToken);
        foreach (ExamAttemptRecord attempt in attempts)
        {
            ExamReport report = Deserialize<ExamReport>(
                attempt.ReportJson,
                "Le rapport d’examen requis par la maîtrise est absent.");
            ExamItemSnapshot[] items = Deserialize<ExamItemSnapshot[]>(
                attempt.FrozenItemsJson,
                "Le tirage d’examen requis par la maîtrise est absent.");
            if (report.AttemptId != attempt.Id
                || report.Status != attempt.Status
                || report.AssistanceDeclared != attempt.AssistanceDeclared)
            {
                throw new InvalidDataException("Le rapport d’examen ne correspond pas à la tentative.");
            }

            foreach (ExamItemReport itemReport in report.Items.Where(item => item.IsAutomaticallyVerified))
            {
                ExamItemSnapshot item = items.Single(candidate =>
                    string.Equals(candidate.ItemId, itemReport.ItemId, StringComparison.Ordinal));
                ExamSubmissionRecord latest = submissions
                    .Where(candidate => candidate.AttemptId == attempt.Id
                        && string.Equals(candidate.ItemId, item.ItemId, StringComparison.Ordinal))
                    .OrderByDescending(candidate => candidate.Sequence)
                    .First();
                bool eligible = !attempt.AssistanceDeclared
                    && attempt.Status is ExamAttemptStatus.Completed or ExamAttemptStatus.TimedOut;
                observations.Add(new MasteryObservation(
                    latest.Id,
                    profileId,
                    item.Domain,
                    MasteryComponent.UnassistedExam,
                    MasteryEvidenceSource.Exam,
                    eligible ? MasteryVerificationKind.ExamEngine : MasteryVerificationKind.ManualDeclaration,
                    item.ItemId,
                    item.ItemVersion,
                    item.ContentRevision,
                    itemReport.Score,
                    eligible ? MasteryAssistance.None : MasteryAssistance.Hint4,
                    report.EndedAtUtc,
                    $"exam:{attempt.Id:N}:{item.ItemId}"));
            }

            if (report.Passed
                && attempt.DurationMinutes >= 90
                && attempt.Status == ExamAttemptStatus.Completed
                && !attempt.AssistanceDeclared)
            {
                achievements.Add(new MasteryAchievement(
                    attempt.Id,
                    profileId,
                    MasteryPolicyCatalog.NinetyMinuteExam,
                    MasteryVerificationKind.ExamEngine,
                    Passed: true,
                    attempt.DurationMinutes,
                    report.EndedAtUtc,
                    $"exam:{attempt.Id:N}"));
            }
        }
    }

    private static MasteryAssistance HintAssistance(int level) => level switch
    {
        <= 0 => MasteryAssistance.None,
        1 => MasteryAssistance.Hint1,
        2 => MasteryAssistance.Hint2,
        3 => MasteryAssistance.Hint3,
        _ => MasteryAssistance.Hint4,
    };

    private static T Deserialize<T>(string? json, string message) =>
        string.IsNullOrWhiteSpace(json)
            ? throw new InvalidDataException(message)
            : JsonSerializer.Deserialize<T>(json, RevisionJsonOptions)
                ?? throw new InvalidDataException(message);

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
