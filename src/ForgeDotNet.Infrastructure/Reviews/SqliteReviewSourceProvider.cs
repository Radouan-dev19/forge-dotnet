using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.Reviews;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Reviews;

public sealed class SqliteReviewSourceProvider(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate,
    IDiagnosticSessionRepository diagnosticSessions,
    IDiagnosticRubricSource diagnosticRubrics) : IReviewSourceProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public async ValueTask<IReadOnlyList<ReviewSourceCandidate>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        var candidates = new List<ReviewSourceCandidate>();
        await AddPersistedActivitySourcesAsync(profileId, candidates, cancellationToken);
        await AddMissedDiagnosticQuestionsAsync(profileId, candidates, cancellationToken);
        return Array.AsReadOnly(candidates
            .OrderBy(item => item.Source.OccurredAtUtc)
            .ThenBy(item => item.Source.Key, StringComparer.Ordinal)
            .ToArray());
    }

    private async Task AddPersistedActivitySourcesAsync(
        Guid profileId,
        List<ReviewSourceCandidate> candidates,
        CancellationToken cancellationToken)
    {
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        PracticeActivityRecord[] practiceActivities = await context.PracticeActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        PracticeLearningAttemptRecord[] practiceFailures = await context.PracticeLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId
                && (item.Status == PracticeLearningAttemptStatus.CompilationFailed
                    || item.Status == PracticeLearningAttemptStatus.TestsFailed
                    || item.Status == PracticeLearningAttemptStatus.TimedOut))
            .ToArrayAsync(cancellationToken);
        foreach (PracticeLearningAttemptRecord failure in practiceFailures
            .GroupBy(item => new { item.ExerciseId, item.ExerciseVersion, item.ContentRevision })
            .Select(group => group.OrderBy(item => item.ObservedAtUtc).First()))
        {
            candidates.Add(SelfAssessed(
                $"practice-error:{failure.ExerciseId}:v{failure.ExerciseVersion}:{failure.ContentRevision}",
                ReviewSourceKind.PracticeError,
                failure.ExerciseId,
                failure.ExerciseVersion,
                failure.ContentRevision,
                failure.ObservedAtUtc,
                MasteryDomain.CSharp,
                $"Reprends l’exercice {failure.ExerciseId} à blanc. Explique d’abord la cause de l’échec, puis vérifie ta correction sans solution."));
        }

        foreach (PracticeActivityRecord activity in practiceActivities.Where(item => item.SolutionViewedAtUtc is not null))
        {
            candidates.Add(SelfAssessed(
                $"practice-solution:{activity.Id:N}:{activity.ContentRevision}",
                ReviewSourceKind.SolutionViewed,
                activity.ExerciseId,
                activity.ExerciseVersion,
                activity.ContentRevision,
                activity.SolutionViewedAtUtc!.Value,
                MasteryDomain.CSharp,
                $"Réimplémente l’exercice {activity.ExerciseId} à blanc, sans rouvrir la solution, puis décris le cas limite qui t’avait bloqué."));
        }

        DebugLabActivityRecord[] debugActivities = await context.DebugLabActivities.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .ToArrayAsync(cancellationToken);
        Guid[] debugActivityIds = debugActivities.Select(item => item.Id).ToArray();
        DebugCorrectionAttemptRecord[] debugFailures = debugActivityIds.Length == 0
            ? Array.Empty<DebugCorrectionAttemptRecord>()
            : await context.DebugCorrectionAttempts.AsNoTracking()
                .Where(item => debugActivityIds.Contains(item.ActivityId)
                    && (item.Outcome == DebugCorrectionOutcome.CompilationFailed
                        || item.Outcome == DebugCorrectionOutcome.TestsFailed
                        || item.Outcome == DebugCorrectionOutcome.TimedOut))
                .ToArrayAsync(cancellationToken);
        foreach (IGrouping<Guid, DebugCorrectionAttemptRecord> group in debugFailures.GroupBy(item => item.ActivityId))
        {
            DebugCorrectionAttemptRecord failure = group.OrderBy(item => item.SubmittedAtUtc).First();
            DebugLabActivityRecord activity = debugActivities.Single(item => item.Id == group.Key);
            candidates.Add(SelfAssessed(
                $"debug-error:{activity.ScenarioId}:v{activity.ScenarioVersion}:{activity.ContentRevision}",
                ReviewSourceKind.DebuggingBug,
                activity.ScenarioId,
                activity.ScenarioVersion,
                activity.ContentRevision,
                failure.SubmittedAtUtc,
                MasteryDomain.Debugging,
                $"Sur {activity.ScenarioId}, reformule symptôme, hypothèse, preuve et test de non-régression avant de recorriger à blanc."));
        }

        foreach (DebugLabActivityRecord activity in debugActivities.Where(item => item.SolutionViewedAtUtc is not null))
        {
            candidates.Add(SelfAssessed(
                $"debug-solution:{activity.Id:N}:{activity.ContentRevision}",
                ReviewSourceKind.SolutionViewed,
                activity.ScenarioId,
                activity.ScenarioVersion,
                activity.ContentRevision,
                activity.SolutionViewedAtUtc!.Value,
                MasteryDomain.Debugging,
                $"Sans rouvrir la correction de {activity.ScenarioId}, reconstruis la cause racine et le test qui empêche la régression."));
        }

        SqlLearningAttemptRecord[] sqlFailures = await context.SqlLearningAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId
                && (item.Status == SqlLabExecutionStatus.Failed
                    || item.Status == SqlLabExecutionStatus.TimedOut
                    || item.Status == SqlLabExecutionStatus.ResultLimitExceeded
                    || (item.Status == SqlLabExecutionStatus.Succeeded
                        && item.ValidationRequested
                        && item.ValidationPassed == false)))
            .ToArrayAsync(cancellationToken);
        foreach (SqlLearningAttemptRecord failure in sqlFailures
            .GroupBy(item => new { item.ScenarioId, item.ScenarioVersion, item.ContentRevision })
            .Select(group => group.OrderBy(item => item.ObservedAtUtc).First()))
        {
            candidates.Add(SelfAssessed(
                $"sql-error:{failure.ScenarioId}:v{failure.ScenarioVersion}:{failure.ContentRevision}",
                ReviewSourceKind.SqlError,
                failure.ScenarioId,
                failure.ScenarioVersion,
                failure.ContentRevision,
                failure.ObservedAtUtc,
                MasteryDomain.Sql,
                $"Rejoue {failure.ScenarioId} sur une base jetable. Explique l’erreur SQL et l’invariant vérifié avant de relancer."));
        }

        ExamAttemptRecord[] examAttempts = await context.ExamAttempts.AsNoTracking()
            .Where(item => item.ProfileId == profileId
                && (item.Status == ExamAttemptStatus.Completed || item.Status == ExamAttemptStatus.TimedOut))
            .ToArrayAsync(cancellationToken);
        foreach (ExamAttemptRecord attempt in examAttempts)
        {
            ExamReport report = Deserialize<ExamReport>(attempt.ReportJson);
            ExamItemSnapshot[] items = Deserialize<ExamItemSnapshot[]>(attempt.FrozenItemsJson);
            foreach (ExamItemReport failure in report.Items.Where(item => item.WasSubmitted && item.Score < 100m))
            {
                ExamItemSnapshot item = items.Single(candidate =>
                    string.Equals(candidate.ItemId, failure.ItemId, StringComparison.Ordinal));
                candidates.Add(SelfAssessed(
                    $"exam-failure:{attempt.Id:N}:{item.ItemId}:{item.ContentRevision}",
                    ReviewSourceKind.ExamFailure,
                    item.ItemId,
                    item.ItemVersion,
                    item.ContentRevision,
                    report.EndedAtUtc,
                    item.Domain,
                    $"Refais à blanc l’item d’examen « {item.Title} ». Explique la cause de l’échec avant de vérifier avec les tests."));
            }
        }
    }

    private async Task AddMissedDiagnosticQuestionsAsync(
        Guid profileId,
        List<ReviewSourceCandidate> candidates,
        CancellationToken cancellationToken)
    {
        DiagnosticSessionData? session = await diagnosticSessions.GetLatestAsync(profileId, cancellationToken);
        if (session is null || session.EndedAtUtc is null)
        {
            return;
        }

        DiagnosticScoringRubric rubric = await diagnosticRubrics.GetRubricAsync(cancellationToken);
        if (!string.Equals(rubric.Snapshot.BankId, session.BankId, StringComparison.Ordinal)
            || rubric.Snapshot.BankVersion != session.BankVersion
            || !string.Equals(rubric.Snapshot.BankRevision, session.BankRevision, StringComparison.Ordinal))
        {
            return;
        }

        Dictionary<string, DiagnosticQuestion> questions = session.Plan.Sections
            .SelectMany(section => section.Questions)
            .ToDictionary(question => question.Id, StringComparer.Ordinal);
        foreach (DiagnosticResponseData response in session.Responses)
        {
            if (!questions.TryGetValue(response.QuestionId, out DiagnosticQuestion? question)
                || !rubric.ExpectedOptions.TryGetValue(response.QuestionId, out string? expectedOption)
                || string.Equals(response.SelectedOptionId, expectedOption, StringComparison.Ordinal))
            {
                continue;
            }

            var source = new ReviewSource(
                $"diagnostic:{session.Id:N}:question:{question.Id}",
                ReviewSourceKind.MissedDiagnosticQuestion,
                question.Id,
                session.BankVersion,
                session.BankRevision,
                response.SavedAtUtc);
            var card = new ReviewCard(
                question.Prompt,
                expectedOption,
                Array.AsReadOnly(question.Options.Select(option => new ReviewChoice(option.Id, option.Text)).ToArray()),
                ReviewEvaluationMode.Choice,
                CanProduceMasteryEvidence: true);
            candidates.Add(new ReviewSourceCandidate(
                source,
                MapDomain(question.Domain),
                ReviewScheduleKind.Recovery,
                card));
        }
    }

    private static ReviewSourceCandidate SelfAssessed(
        string key,
        ReviewSourceKind kind,
        string itemId,
        int version,
        string revision,
        DateTimeOffset occurredAtUtc,
        MasteryDomain domain,
        string question) => new(
            new ReviewSource(key, kind, itemId, version, revision, occurredAtUtc),
            domain,
            ReviewScheduleKind.Recovery,
            new ReviewCard(
                question,
                null,
                Array.Empty<ReviewChoice>(),
                ReviewEvaluationMode.SelfAssessment,
                CanProduceMasteryEvidence: false));

    private static MasteryDomain MapDomain(DiagnosticDomain domain) => domain switch
    {
        DiagnosticDomain.Logic => MasteryDomain.CSharp,
        DiagnosticDomain.CSharp => MasteryDomain.CSharp,
        DiagnosticDomain.Reading => MasteryDomain.CSharp,
        DiagnosticDomain.Debugging => MasteryDomain.Debugging,
        DiagnosticDomain.Sql => MasteryDomain.Sql,
        DiagnosticDomain.Http => MasteryDomain.Api,
        DiagnosticDomain.Git => MasteryDomain.ContinuousIntegration,
        DiagnosticDomain.Testing => MasteryDomain.Tests,
        DiagnosticDomain.English => MasteryDomain.English,
        _ => throw new ArgumentOutOfRangeException(nameof(domain)),
    };

    private static T Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? throw new InvalidDataException("Une source d’examen requise par les révisions est absente.")
            : JsonSerializer.Deserialize<T>(json, SerializerOptions)
                ?? throw new InvalidDataException("Une source d’examen requise par les révisions est illisible.");

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
