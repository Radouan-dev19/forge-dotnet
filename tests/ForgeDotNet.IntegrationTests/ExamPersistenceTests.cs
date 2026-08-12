using System.Text.Json;
using ForgeDotNet.Application.Analytics;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.Analytics;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;
using ForgeDotNet.Infrastructure.Persistence;
using ForgeDotNet.Infrastructure.Reviews;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.IntegrationTests;

[Trait("Category", "ExamIntegrity")]
public sealed class ExamPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ActiveReportIsDeferredHiddenResultIsRedactedAndFailureFeedsMasteryAndReview()
    {
        string dataDirectory;
        Guid attemptId;
        var clock = new MutableTimeProvider(Now);
        await using (var environment = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        {
            dataDirectory = environment.DataDirectory;
            ExamService service = CreateService(
                environment,
                clock,
                DeterministicRunScenario.HiddenTestFailure);
            ExamAttemptView active = await service.StartAsync("reference-exam-v1");
            attemptId = active.Id;

            Assert.Null(active.Report);
            Assert.True(active.CanResume);
            Assert.DoesNotContain("DrawSeed", JsonSerializer.Serialize(active), StringComparison.Ordinal);
            Assert.True(await environment.GetRequiredService<IExamAccessPolicy>()
                .IsLearningAidLockedAsync(
                    (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId,
                    clock.GetUtcNow()));
            Assert.Null(await environment.GetRequiredService<IExamRepository>().GetReportAsync(
                (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId,
                active.Id));

            ExamItemView item = Assert.Single(active.Items);
            ExamSubmissionReceiptView receipt = await service.SubmitAsync(
                active.Id,
                active.Version,
                item.ItemId,
                item.SourceCode + Environment.NewLine + "// tentative");
            string receiptJson = JsonSerializer.Serialize(receipt);
            Assert.DoesNotContain("Hidden", receiptJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Failed", receiptJson, StringComparison.OrdinalIgnoreCase);
            ExamAttemptView beforeFinish = await service.GetAttemptAsync(active.Id);
            Assert.Null(beforeFinish.Report);

            ExamAttemptView finished = await service.FinishAsync(
                active.Id,
                beforeFinish.Version,
                assistanceDeclared: false);
            Assert.NotNull(finished.Report);
            ExamReportView report = finished.Report;
            Assert.False(report.Passed);
            Assert.True(Assert.Single(report.Items).HiddenFailureCount > 0);
            Assert.DoesNotContain("expected", JsonSerializer.Serialize(report), StringComparison.OrdinalIgnoreCase);
            Assert.False(await environment.GetRequiredService<IExamAccessPolicy>()
                .IsLearningAidLockedAsync(
                    (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId,
                    clock.GetUtcNow()));

            Guid profileId = (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
            MasteryEvidenceSet mastery = await environment.GetRequiredService<IMasteryEvidenceSource>().ReadAsync(profileId);
            MasteryObservation examEvidence = Assert.Single(
                mastery.Observations,
                observation => observation.Source == MasteryEvidenceSource.Exam);
            Assert.Equal(MasteryVerificationKind.ExamEngine, examEvidence.Verification);

            var reviewProvider = new SqliteReviewSourceProvider(
                environment.GetRequiredService<IDbContextFactory<ForgeDbContext>>(),
                environment.GetRequiredService<LocalDatabaseGate>(),
                new EmptyDiagnosticRepository(),
                new UnusedRubricSource(),
                new UnusedReviewCardSource());
            ReviewSourceCandidate review = Assert.Single(
                await reviewProvider.ListAsync(profileId),
                candidate => candidate.Source.Kind == ReviewSourceKind.ExamFailure);
            Assert.False(review.Card.CanProduceMasteryEvidence);
            Assert.Null(review.Card.ExpectedAnswer);

            AnalyticsEvidence analytics = await environment.GetRequiredService<IAnalyticsEvidenceSource>().ReadAsync(profileId);
            AnalyticsSnapshot snapshot = AnalyticsRules.Calculate(analytics, TimeSpan.FromMinutes(5), Now.AddHours(1));
            Assert.Equal(1, snapshot.CompletedExamCount);
            Assert.NotNull(snapshot.AverageExamScore);
        }

        await using var restarted = await PersistenceTestEnvironment.CreateAsync(dataDirectory, timeProvider: clock);
        Guid restoredProfile = (await restarted.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
        ExamReport? restoredReport = await restarted.GetRequiredService<IExamRepository>()
            .GetReportAsync(restoredProfile, attemptId);
        Assert.NotNull(restoredReport);
        Assert.Equal(ExamAttemptStatus.Completed, restoredReport.Status);
    }

    [Fact]
    public async Task ConcurrentDoubleFinishStoresOneImmutableReport()
    {
        var clock = new MutableTimeProvider(Now);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        ExamService service = CreateService(environment, clock, DeterministicRunScenario.Successful);
        ExamAttemptView active = await service.StartAsync("reference-exam-v1");
        ExamItemView item = Assert.Single(active.Items);
        _ = await service.SubmitAsync(active.Id, active.Version, item.ItemId, item.SourceCode);
        ExamAttemptView ready = await service.GetAttemptAsync(active.Id);

        Exception? first = null;
        Exception? second = null;
        await Task.WhenAll(
            Task.Run(async () => first = await Record.ExceptionAsync(() =>
                service.FinishAsync(ready.Id, ready.Version, assistanceDeclared: false).AsTask())),
            Task.Run(async () => second = await Record.ExceptionAsync(() =>
                service.FinishAsync(ready.Id, ready.Version, assistanceDeclared: false).AsTask())));

        Assert.Equal(1, new[] { first, second }.Count(exception => exception is null));
        Assert.Equal(1, new[] { first, second }.Count(exception => exception is InvalidOperationException));
        Guid profileId = (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
        Assert.NotNull(await environment.GetRequiredService<IExamRepository>().GetReportAsync(profileId, ready.Id));
    }

    [Fact]
    public async Task DeadlineClosesAttemptAndEmptyAnalyticsRemainUnavailable()
    {
        var clock = new MutableTimeProvider(Now);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        Guid profileId = (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
        AnalyticsEvidence empty = await environment.GetRequiredService<IAnalyticsEvidenceSource>().ReadAsync(profileId);
        AnalyticsSnapshot emptySnapshot = AnalyticsRules.Calculate(empty, TimeSpan.FromMinutes(5), Now);
        Assert.Null(emptySnapshot.FirstAttemptSuccessRate);
        Assert.Null(emptySnapshot.AverageExamScore);

        ExamService service = CreateService(environment, clock, DeterministicRunScenario.Successful);
        ExamAttemptView active = await service.StartAsync("reference-exam-v1");
        clock.Advance(TimeSpan.FromMinutes(31));

        ExamAttemptView expired = await service.GetAttemptAsync(active.Id);

        Assert.Equal(ExamAttemptStatus.TimedOut, expired.Status);
        Assert.NotNull(expired.Report);
        Assert.False(expired.CanResume);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(
            active.Id,
            active.Version,
            active.Items[0].ItemId,
            active.Items[0].SourceCode).AsTask());
    }

    [Fact]
    public async Task SqlSubmissionUsesSqlRunnerAndProducesSqlMasteryEvidence()
    {
        var clock = new MutableTimeProvider(Now);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        var sqlRunner = new RecordingSqlExamRunner();
        var service = new ExamService(
            new StaticExamBankSource(SqlBlueprint()),
            environment.GetRequiredService<IExamRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            new ThrowingCodeRunner(),
            sqlRunner,
            clock);

        ExamAttemptView active = await service.StartAsync("sql-exam-v1");
        ExamItemView item = Assert.Single(active.Items);
        _ = await service.SubmitAsync(active.Id, active.Version, item.ItemId, "SELECT 42 AS Value;");
        ExamAttemptView ready = await service.GetAttemptAsync(active.Id);
        ExamAttemptView completed = await service.FinishAsync(
            ready.Id,
            ready.Version,
            assistanceDeclared: false);

        Assert.Equal(1, sqlRunner.CallCount);
        Assert.True(completed.Report?.Passed);
        Assert.Equal("SQL", Assert.Single(completed.Report!.Items).DomainLabel);
        Guid profileId = (await environment.GetRequiredService<ILocalProfileRepository>().GetAsync()).LocalId;
        MasteryEvidenceSet evidence = await environment.GetRequiredService<IMasteryEvidenceSource>().ReadAsync(profileId);
        MasteryObservation observation = Assert.Single(
            evidence.Observations,
            value => value.Source == MasteryEvidenceSource.Exam);
        Assert.Equal(MasteryDomain.Sql, observation.Domain);
        Assert.Equal(MasteryVerificationKind.ExamEngine, observation.Verification);
    }

    private static ExamService CreateService(
        PersistenceTestEnvironment environment,
        TimeProvider clock,
        DeterministicRunScenario scenario) => new(
            new StaticExamBankSource(Blueprint()),
            environment.GetRequiredService<IExamRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            new DeterministicCodeRunner(
                new DeterministicCodeRunnerOptions { Scenarios = Array.AsReadOnly([scenario]) },
                clock),
            new UnavailableSqlExamRunner(),
            clock);

    private static ExamBlueprint Blueprint() => new(
        "reference-exam-v1",
        1,
        new string('a', 64),
        "Examen de référence",
        30,
        1,
        100m,
        [new ExamCandidate(
            "reference-total-001",
            1,
            new string('b', 64),
            MasteryDomain.CSharp,
            "Total de référence",
            "Implémente le calcul demandé sans aide.",
            Array.Empty<string>(),
            "Submission.cs",
            "public static class Submission { public static int Run() => 1; }")]);

    private static ExamBlueprint SqlBlueprint() => new(
        "sql-exam-v1",
        1,
        new string('c', 64),
        "Examen SQL",
        30,
        1,
        100m,
        [new ExamCandidate(
            "exam-sql-test-001",
            1,
            new string('d', 64),
            MasteryDomain.Sql,
            "Retourner 42",
            "Retournez la valeur 42.",
            [],
            "Submission.sql",
            "SELECT 0 AS Value;",
            ExamSubmissionKind.Sql)]);

    private sealed class StaticExamBankSource(ExamBlueprint blueprint) : IExamBankSource
    {
        public ValueTask<IReadOnlyList<ExamBlueprint>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<ExamBlueprint>>([blueprint]);

        public ValueTask<ExamBlueprint?> GetAsync(string examId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ExamBlueprint?>(string.Equals(examId, blueprint.Id, StringComparison.Ordinal)
                ? blueprint
                : null);
    }

    private sealed class RecordingSqlExamRunner : ISqlExamRunner
    {
        public int CallCount { get; private set; }

        public ValueTask<ExamRunResult> RunAsync(
            ExamItemSnapshot item,
            string query,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Assert.Equal(ExamSubmissionKind.Sql, item.SubmissionKind);
            Assert.Equal("SELECT 42 AS Value;", query);
            return ValueTask.FromResult(new ExamRunResult(
                ExamSubmissionOutcome.Succeeded,
                2,
                2,
                0,
                Guid.NewGuid()));
        }
    }

    private sealed class ThrowingCodeRunner : ForgeDotNet.Application.CodeRunner.ICodeRunner
    {
        public ValueTask<ForgeDotNet.Application.CodeRunner.CodeRunResult> RunAsync(
            ForgeDotNet.Application.CodeRunner.CodeRunRequest request,
            CancellationToken cancellationToken = default) =>
            throw new Xunit.Sdk.XunitException("Le CodeRunner C# ne doit pas recevoir une soumission SQL.");
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }

    private sealed class EmptyDiagnosticRepository : ForgeDotNet.Application.Diagnostic.IDiagnosticSessionRepository
    {
        public ValueTask<ForgeDotNet.Application.Diagnostic.DiagnosticSessionData?> GetLatestAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) => ValueTask.FromResult<ForgeDotNet.Application.Diagnostic.DiagnosticSessionData?>(null);

        public ValueTask<ForgeDotNet.Application.Diagnostic.DiagnosticSessionData?> GetAsync(Guid profileId, Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ForgeDotNet.Application.Diagnostic.DiagnosticSessionData?> GetActiveAsync(Guid profileId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ForgeDotNet.Application.Diagnostic.DiagnosticSessionData> CreateOrGetActiveAsync(ForgeDotNet.Application.Diagnostic.DiagnosticSessionData value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask SaveTimelineAsync(Guid profileId, Guid sessionId, ForgeDotNet.Domain.Diagnostic.DiagnosticTimeline timeline, DateTimeOffset updatedAtUtc, DateTimeOffset? endedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask UpsertResponseAsync(Guid profileId, Guid sessionId, ForgeDotNet.Application.Diagnostic.DiagnosticResponseData response, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnusedRubricSource : ForgeDotNet.Application.Diagnostic.IDiagnosticRubricSource
    {
        public ValueTask<ForgeDotNet.Domain.Diagnostic.DiagnosticScoringRubric> GetRubricAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>Ce scénario ne pratique aucun exercice : aucune carte n'est attendue.</summary>
    private sealed class UnusedReviewCardSource : ForgeDotNet.Application.Reviews.IReviewCardSource
    {
        public ValueTask<IReadOnlyList<ForgeDotNet.Application.Reviews.ExerciseReviewCard>> GetForExerciseAsync(
            string exerciseId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<ForgeDotNet.Application.Reviews.ExerciseReviewCard>>([]);
    }
}
