using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Application.Practice;
using ForgeDotNet.Application.Projects;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.Projects;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Mastery;

public sealed class SqliteMasteryEvidenceSource(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate,
    IProjectSource? projectSource = null,
    IPracticeExerciseSource? exerciseSource = null,
    ContentCatalogProvider? catalogProvider = null) : IMasteryEvidenceSource
{
    /// <summary>Préfixe des activités de lecture qui portent une réussite de quiz.</summary>
    private const string QuizActivityPrefix = "quiz:";

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
        await AddProjectsAsync(context, profileId, achievements, cancellationToken);
        await AddQuizAsync(context, profileId, observations, cancellationToken);
        await AddHumanAttestationsAsync(context, profileId, observations, achievements, cancellationToken);
        MasteryObservation[] ordered = observations.OrderBy(item => item.Id).ToArray();
        MasteryAchievement[] orderedAchievements = achievements.OrderBy(item => item.Id).ToArray();
        string json = JsonSerializer.Serialize(new { ordered, orderedAchievements }, RevisionJsonOptions);
        string revision = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}";
        return new MasteryEvidenceSet(
            Array.AsReadOnly(ordered),
            Array.AsReadOnly(orderedAchievements),
            revision);
    }

    /// <summary>
    /// Domaine alimenté par un exercice, déduit de sa première compétence.
    /// </summary>
    /// <remarks>
    /// Cette méthode remplace un littéral <c>MasteryDomain.CSharp</c> qui attribuait toute pratique
    /// au même domaine : les quatorze exercices d'API alimentaient le score C#, et le domaine Api
    /// plafonnait à quinze pour un seuil de quatre-vingt-cinq. Sans source d'exercices — les doubles
    /// de test n'en fournissent pas toujours — le comportement d'origine est conservé, ce qui garde
    /// les observations existantes lisibles.
    /// </remarks>
    private async ValueTask<MasteryDomain> PracticeDomainAsync(
        string exerciseId,
        CancellationToken cancellationToken)
    {
        if (exerciseSource is null)
        {
            return MasteryDomain.CSharp;
        }

        PracticeExercise? exercise = await exerciseSource.GetAsync(exerciseId, cancellationToken);
        return exercise is null
            ? MasteryDomain.CSharp
            : MasterySkillDomains.FromSkills(exercise.Skills);
    }

    private async Task AddPracticeAsync(
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
                await PracticeDomainAsync(activity.ExerciseId, cancellationToken),
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
                await PracticeDomainAsync(attempt.ExerciseId, cancellationToken),
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

    /// <summary>
    /// Transforme une soumission de projet réussie en accomplissement de maîtrise.
    /// </summary>
    /// <remarks>
    /// Quatre conditions, toutes nécessaires, aucune contournable depuis l'interface :
    /// la soumission déclare une réussite, elle a été réellement exécutée dans le bac à sable —
    /// <c>AutomaticallyVerified</c>, refusé par le domaine pour une déclaration manuelle —, toutes
    /// ses suites d'acceptation sont passées, et le projet déclare la clé d'exigence qu'il satisfait.
    ///
    /// La vérification est enregistrée comme <see cref="MasteryVerificationKind.AutomaticTests"/>,
    /// que <c>MasteryRules.IsVerifiedAchievement</c> accepte déjà. Aucune règle de maîtrise n'est
    /// modifiée par ce producteur : c'est le chaînon qui manquait, pas la règle.
    /// </remarks>
    private async Task AddProjectsAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryAchievement> achievements,
        CancellationToken cancellationToken)
    {
        if (projectSource is null)
        {
            return;
        }

        ProjectSubmissionRecord[] submissions = await context.ProjectSubmissions.AsNoTracking()
            .Where(item => item.ProfileId == profileId
                && item.Status == ProjectSubmissionStatus.Succeeded
                && item.AutomaticallyVerified
                && item.TotalSuites > 0
                && item.PassedSuites == item.TotalSuites
                && item.TotalTests > 0
                && item.PassedTests == item.TotalTests)
            .ToArrayAsync(cancellationToken);
        if (submissions.Length == 0)
        {
            return;
        }

        foreach (ProjectSubmissionRecord submission in submissions)
        {
            Project? project = await projectSource.GetAsync(submission.ProjectId, cancellationToken);
            if (project is null || !project.ProducesAchievement || project.Version != submission.ProjectVersion)
            {
                continue;
            }

            achievements.Add(new MasteryAchievement(
                submission.Id,
                profileId,
                project.AchievementKey!,
                MasteryVerificationKind.AutomaticTests,
                Passed: true,
                DurationMinutes: 0,
                submission.ObservedAtUtc,
                $"project:{submission.Id:N}"));
        }
    }

    /// <summary>
    /// Projette les attestations humaines enregistrées : accomplissement pour les six exigences à
    /// jugement humain, observation d'explication pour la septième grille.
    /// </summary>
    /// <remarks>
    /// Le type de vérification est toujours <see cref="MasteryVerificationKind.HumanAttestation"/> :
    /// la projection ne décide pas de sa valeur, ce sont les règles qui l'admettent — exclusivement
    /// pour les clés que <see cref="MasteryPolicyCatalog.HumanJudgementKeys"/> déclare, et pour la
    /// composante Explication. Une attestation enregistrée sur toute autre clé resterait visible et
    /// vaudrait zéro, exactement comme une déclaration manuelle.
    /// </remarks>
    private async Task AddHumanAttestationsAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        List<MasteryAchievement> achievements,
        CancellationToken cancellationToken)
    {
        HumanAttestationRecord[] attestations = await context.HumanAttestations.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        foreach (HumanAttestationRecord attestation in attestations)
        {
            DateTimeOffset observedAtUtc = new(
                attestation.ReviewedOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            if (string.Equals(attestation.TargetKey, HumanReviewCatalog.ExplanationTarget, StringComparison.Ordinal))
            {
                string exerciseId = attestation.ExplainedExerciseId ?? string.Empty;
                if (exerciseId.Length == 0)
                {
                    continue;
                }

                observations.Add(new MasteryObservation(
                    attestation.Id,
                    profileId,
                    await PracticeDomainAsync(exerciseId, cancellationToken),
                    MasteryComponent.Explanation,
                    MasteryEvidenceSource.Explanation,
                    MasteryVerificationKind.HumanAttestation,
                    exerciseId,
                    ItemVersion: 1,
                    ContentRevision: "human-review-v1",
                    Score: 100m,
                    MasteryAssistance.None,
                    observedAtUtc,
                    $"attestation:{attestation.Id:N}"));
                continue;
            }

            achievements.Add(new MasteryAchievement(
                attestation.Id,
                profileId,
                attestation.TargetKey,
                MasteryVerificationKind.HumanAttestation,
                Passed: true,
                attestation.DurationMinutes,
                observedAtUtc,
                $"attestation:{attestation.Id:N}"));
        }
    }

    /// <summary>
    /// Transforme une réussite de quiz de leçon en observation de maîtrise.
    /// </summary>
    /// <remarks>
    /// La composante quiz pèse cinq pour cent et n'avait aucun producteur : son poids n'étant jamais
    /// redistribué, il plafonnait chaque domaine cinq points sous ce qu'il aurait dû atteindre. Le
    /// quiz est pourtant déjà corrigé côté serveur — <c>SubmitLessonQuiz</c> compare à l'option
    /// attendue et n'écrit l'activité qu'en cas de réussite. Il ne manquait que la projection.
    ///
    /// Limite assumée : seule une réussite est persistée, donc une réponse juste au cinquième essai
    /// vaut la première. À cinq pour cent de poids, et sous la règle « accumulation de quiz faciles →
    /// poids maximal cinq pour cent » de la matrice anti-contournement, l'effet reste borné. Le dire
    /// vaut mieux que le masquer.
    /// </remarks>
    private async Task AddQuizAsync(
        ForgeDbContext context,
        Guid profileId,
        List<MasteryObservation> observations,
        CancellationToken cancellationToken)
    {
        if (catalogProvider is null)
        {
            return;
        }

        LessonReadingActivityRecord[] activities = await context.LessonReadingActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.ActivityId.StartsWith(QuizActivityPrefix))
            .ToArrayAsync(cancellationToken);
        if (activities.Length == 0)
        {
            return;
        }

        ContentCatalog catalog = catalogProvider.Current;
        foreach (LessonReadingActivityRecord activity in activities)
        {
            ContentCatalogItem? lesson = catalog.FindById(activity.LessonId);
            if (lesson is null || lesson.Type != ContentDocumentType.Lesson || lesson.Skills.Count == 0)
            {
                continue;
            }

            observations.Add(new MasteryObservation(
                // L'activité n'a pas d'identité propre : le couple profil, leçon et activité en
                // fournit une, stable d'un recalcul à l'autre.
                DeterministicId(profileId, activity.LessonId, activity.ActivityId),
                profileId,
                MasterySkillDomains.FromSkills(lesson.Skills),
                MasteryComponent.Quiz,
                MasteryEvidenceSource.Quiz,
                MasteryVerificationKind.QuizEngine,
                activity.LessonId,
                lesson.Version,
                catalog.Revision,
                100m,
                MasteryAssistance.None,
                activity.CompletedAtUtc,
                $"quiz:{activity.LessonId}:{activity.ActivityId}"));
        }
    }

    /// <summary>
    /// Identifiant reproductible d'une observation qui n'en porte pas dans la base.
    /// </summary>
    private static Guid DeterministicId(Guid profileId, string lessonId, string activityId) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"{profileId:N}:{lessonId}:{activityId}")).AsSpan(0, 16));

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
