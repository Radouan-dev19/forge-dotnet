using System.Globalization;
using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.Reviews;
using ForgeDotNet.Infrastructure.Persistence;
using ForgeDotNet.Infrastructure.Reviews;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.IntegrationTests;

[Trait("Category", "ReviewScheduling")]
public sealed class ReviewSourceProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ErrorsBugsMissedQuestionsAndSolutionsBecomeDeduplicatedPrivateCandidates()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        Guid profileId = (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
        await SeedSourcesAsync(environment.DatabasePath, profileId);
        DiagnosticSessionData session = CreateMissedQuestionSession(profileId);
        DiagnosticScoringRubric rubric = CreateRubric(session);
        var provider = new SqliteReviewSourceProvider(
            environment.GetRequiredService<IDbContextFactory<ForgeDbContext>>(),
            environment.GetRequiredService<LocalDatabaseGate>(),
            new StaticDiagnosticSessionRepository(session),
            new StaticRubricSource(rubric),
            new EmptyReviewCardSource());

        IReadOnlyList<ReviewSourceCandidate> sources = await provider.ListAsync(profileId);

        Assert.Contains(sources, item => item.Source.Kind == ReviewSourceKind.PracticeError);
        Assert.Contains(sources, item => item.Source.Kind == ReviewSourceKind.DebuggingBug);
        Assert.Contains(sources, item => item.Source.Kind == ReviewSourceKind.SqlError);
        Assert.Contains(sources, item => item.Source.Kind == ReviewSourceKind.SolutionViewed);
        ReviewSourceCandidate missed = Assert.Single(
            sources,
            item => item.Source.Kind == ReviewSourceKind.MissedDiagnosticQuestion);
        Assert.True(missed.Card.CanProduceMasteryEvidence);
        Assert.Equal("b", missed.Card.ExpectedAnswer);
        Assert.Equal(2, missed.Card.Choices.Count);
        Assert.Equal(
            1,
            sources.Count(item => item.Source.Kind == ReviewSourceKind.PracticeError));
        Assert.Equal(sources.Count, sources.Select(item => item.Source.Key).Distinct(StringComparer.Ordinal).Count());
    }

    private static DiagnosticSessionData CreateMissedQuestionSession(Guid profileId)
    {
        var question = new DiagnosticQuestion(
            "diag-q1",
            DiagnosticDomain.CSharp,
            2,
            "Quel choix est correct ?",
            [new("a", "Option fausse"), new("b", "Option correcte")]);
        var plan = new DiagnosticPlan(
            DiagnosticMode.Reduced,
            7,
            [new DiagnosticPlanSection(0, "C#", [question])]);
        return new DiagnosticSessionData(
            Guid.NewGuid(),
            profileId,
            "bank-v1",
            1,
            new string('b', 64),
            plan,
            new DiagnosticTimeline(
                DiagnosticSessionStatus.Completed,
                1,
                [DiagnosticSectionStatus.Completed],
                null,
                null),
            120,
            Now.AddHours(-1),
            Now,
            Now,
            [new DiagnosticResponseData(question.Id, "a", Now.AddMinutes(-10))]);
    }

    private static DiagnosticScoringRubric CreateRubric(DiagnosticSessionData session) => new(
        new DiagnosticRubricSnapshot(
            "rubric-v1",
            1,
            new string('r', 64),
            session.BankId,
            session.BankVersion,
            session.BankRevision,
            [],
            [],
            50m,
            40m,
            60m,
            80m,
            1.96),
        new Dictionary<string, string>(StringComparer.Ordinal) { ["diag-q1"] = "b" });

    private static async Task SeedSourcesAsync(string databasePath, Guid profileId)
    {
        Guid debugActivityId = Guid.NewGuid();
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PracticeLearningAttempts
                (Id, ProfileId, ExerciseId, ExerciseVersion, ContentRevision, SubmissionFingerprint,
                 Status, TotalTests, PassedTests, DiagnosticId, ObservedAtUtc)
            VALUES
                ($practice1, $profile, 'practice-review-source', 1, $revision, $fingerprint,
                 'TestsFailed', 3, 1, $diagnostic1, $then),
                ($practice2, $profile, 'practice-review-source', 1, $revision, $fingerprint,
                 'CompilationFailed', 0, 0, $diagnostic2, $now);

            INSERT INTO PracticeActivities
                (Id, ProfileId, ExerciseId, ExerciseVersion, ContentRevision, Version, State,
                 StartedAtUtc, SolutionViewedAtUtc)
            VALUES
                ($activity, $profile, 'practice-solution-source', 1, $revision, 3,
                 'SolutionViewed', $then, $now);

            INSERT INTO DebugLabActivities
                (Id, ProfileId, ScenarioId, ScenarioVersion, ContentRevision, Version, State, StartedAtUtc,
                 Symptom, Context, Hypotheses, Evidence, Cause, Fix, Test, Prevention)
            VALUES
                ($debugActivity, $profile, 'debug-review-source', 1, $revision, 2, 'CorrectionReady', $then,
                 'symptôme', 'contexte', 'hypothèse', 'preuve', '', '', '', '');
            INSERT INTO DebugCorrectionAttempts
                (Id, ActivityId, Sequence, SourceFingerprint, Outcome, TotalTests, PassedTests, FailedTests,
                 DiagnosticId, SubmittedAtUtc)
            VALUES
                ($debugAttempt, $debugActivity, 1, $fingerprint, 'TestsFailed', 2, 1, 1,
                 $debugDiagnostic, $now);

            INSERT INTO SqlLearningAttempts
                (Id, ProfileId, ScenarioId, ScenarioVersion, ContentRevision, Status, ValidationRequested,
                 ValidationPassed, QueryFingerprint, DiagnosticId, ObservedAtUtc, ElapsedMilliseconds)
            VALUES
                ($sqlAttempt, $profile, 'sql-review-source', 1, $revision, 'Succeeded', 1, 0,
                 $queryFingerprint, $sqlDiagnostic, $now, 12);
            """;
        command.Parameters.AddWithValue("$practice1", Guid.NewGuid());
        command.Parameters.AddWithValue("$practice2", Guid.NewGuid());
        command.Parameters.AddWithValue("$activity", Guid.NewGuid());
        command.Parameters.AddWithValue("$debugActivity", debugActivityId);
        command.Parameters.AddWithValue("$debugAttempt", Guid.NewGuid());
        command.Parameters.AddWithValue("$sqlAttempt", Guid.NewGuid());
        command.Parameters.AddWithValue("$profile", profileId);
        command.Parameters.AddWithValue("$diagnostic1", Guid.NewGuid());
        command.Parameters.AddWithValue("$diagnostic2", Guid.NewGuid());
        command.Parameters.AddWithValue("$debugDiagnostic", Guid.NewGuid());
        command.Parameters.AddWithValue("$sqlDiagnostic", Guid.NewGuid());
        command.Parameters.AddWithValue("$revision", new string('c', 64));
        command.Parameters.AddWithValue("$fingerprint", $"sha256:{new string('d', 64)}");
        command.Parameters.AddWithValue("$queryFingerprint", new string('e', 64));
        command.Parameters.AddWithValue("$then", Now.AddMinutes(-20).ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$now", Now.ToString("O", CultureInfo.InvariantCulture));
        _ = await command.ExecuteNonQueryAsync();
    }

    private sealed class StaticRubricSource(DiagnosticScoringRubric rubric) : IDiagnosticRubricSource
    {
        public ValueTask<DiagnosticScoringRubric> GetRubricAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(rubric);
    }

    private sealed class StaticDiagnosticSessionRepository(DiagnosticSessionData session) : IDiagnosticSessionRepository
    {
        public ValueTask<DiagnosticSessionData?> GetLatestAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) => ValueTask.FromResult<DiagnosticSessionData?>(session);

        public ValueTask<DiagnosticSessionData?> GetAsync(Guid profileId, Guid sessionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<DiagnosticSessionData?> GetActiveAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<DiagnosticSessionData> CreateOrGetActiveAsync(DiagnosticSessionData value, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask SaveTimelineAsync(Guid profileId, Guid sessionId, DiagnosticTimeline timeline, DateTimeOffset updatedAtUtc, DateTimeOffset? endedAtUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask UpsertResponseAsync(Guid profileId, Guid sessionId, DiagnosticResponseData response, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Banque vide : ce test porte sur les sources issues de l'activité persistée, pas sur les
    /// cartes d'exercice, qui ont leur propre couverture.
    /// </summary>
    private sealed class EmptyReviewCardSource : IReviewCardSource
    {
        public ValueTask<IReadOnlyList<ExerciseReviewCard>> GetForExerciseAsync(
            string exerciseId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<ExerciseReviewCard>>([]);
    }
}
