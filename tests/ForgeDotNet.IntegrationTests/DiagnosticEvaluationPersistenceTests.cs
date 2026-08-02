using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Infrastructure.Diagnostic;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class DiagnosticEvaluationPersistenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 26, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompletedReportIsCreatedOnceAndSurvivesRubricSourceChange()
    {
        string dataDirectory;
        Guid sessionId;
        DiagnosticEvaluationView original;
        var clock = new FixedTimeProvider(Start);

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        {
            dataDirectory = firstRun.DataDirectory;
            using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
            using var coordinator = new DiagnosticSessionCoordinator();
            DiagnosticSessionService sessions = CreateSessionService(firstRun, source, coordinator, clock);
            DiagnosticSessionView completed = await CompleteReducedAsync(sessions, source);
            sessionId = completed.Id;
            DiagnosticEvaluationService evaluations = CreateEvaluationService(
                firstRun,
                source,
                coordinator,
                clock);

            DiagnosticEvaluationView[] concurrent = await Task.WhenAll(
                evaluations.GetOrCreateAsync(sessionId).AsTask(),
                evaluations.GetOrCreateAsync(sessionId).AsTask());
            original = concurrent[0];

            Assert.Equal(original.CreatedAtUtc, concurrent[1].CreatedAtUtc);
            Assert.Equal(100m, original.Score);
            Assert.Equal(DiagnosticConfidence.Low, original.Confidence);
            Assert.True(original.IsProvisional);
            Assert.Empty(original.CriticalGaps);
            await using var connection = new SqliteConnection($"Data Source={firstRun.DatabasePath};Mode=ReadOnly");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT FrozenRubricJson, ReportJson FROM DiagnosticEvaluations WHERE SessionId = $sessionId;";
            command.Parameters.AddWithValue("$sessionId", sessionId);
            await using SqliteDataReader reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            string persistedText = reader.GetString(0) + reader.GetString(1);
            Assert.DoesNotContain("expectedOption", persistedText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("diag-", persistedText, StringComparison.OrdinalIgnoreCase);
            Assert.False(await reader.ReadAsync());
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(
            dataDirectory,
            timeProvider: clock);
        using var secondCoordinator = new DiagnosticSessionCoordinator();
        DiagnosticEvaluationService secondService = CreateEvaluationService(
            secondRun,
            new ThrowingRubricSource(),
            secondCoordinator,
            clock);

        DiagnosticEvaluationView restored = await secondService.GetOrCreateAsync(sessionId);

        Assert.Equal(original.Score, restored.Score);
        Assert.Equal(original.LowerBound, restored.LowerBound);
        Assert.Equal(original.RubricRevision, restored.RubricRevision);
        Assert.Equal(original.CreatedAtUtc, restored.CreatedAtUtc);
        Assert.Equal(
            original.Domains.Select(domain => domain.Score),
            restored.Domains.Select(domain => domain.Score));
    }

    [Fact]
    public async Task ActiveSessionCannotExposeAnswerKeyThroughEvaluation()
    {
        var clock = new FixedTimeProvider(Start);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
        using var coordinator = new DiagnosticSessionCoordinator();
        DiagnosticSessionService sessions = CreateSessionService(environment, source, coordinator, clock);
        DiagnosticSessionView active = await sessions.StartAsync(DiagnosticMode.Reduced);
        DiagnosticEvaluationService evaluations = CreateEvaluationService(
            environment,
            source,
            coordinator,
            clock);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            evaluations.GetOrCreateAsync(active.Id).AsTask());

        Assert.Contains("terminé ou abandonné", exception.Message, StringComparison.Ordinal);
        var profile = await environment.GetRequiredService<ILocalProfileRepository>().GetAsync();
        Assert.Null(await environment
            .GetRequiredService<IDiagnosticEvaluationRepository>()
            .GetAsync(profile.LocalId, active.Id));
    }

    [Fact]
    public async Task AbandonedSessionProducesExplicitlyInsufficientReport()
    {
        var clock = new FixedTimeProvider(Start);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
        using var coordinator = new DiagnosticSessionCoordinator();
        DiagnosticSessionService sessions = CreateSessionService(environment, source, coordinator, clock);
        DiagnosticSessionView active = await sessions.StartAsync(DiagnosticMode.Reduced);
        DiagnosticSessionView abandoned = await sessions.AbandonAsync(active.Id);
        DiagnosticEvaluationService evaluations = CreateEvaluationService(
            environment,
            source,
            coordinator,
            clock);

        DiagnosticEvaluationView report = await evaluations.GetOrCreateAsync(abandoned.Id);

        Assert.Equal(0m, report.Score);
        Assert.Equal(0m, report.LowerBound);
        Assert.Equal(100m, report.UpperBound);
        Assert.Equal(DiagnosticConfidence.Insufficient, report.Confidence);
        Assert.Equal(DiagnosticLevel.EvidenceInsufficient, report.Level);
        Assert.Equal(5, report.CriticalGaps.Count);
        Assert.All(report.CriticalGaps, gap =>
            Assert.Equal(DiagnosticCriticalGapReason.MissingEvidence, gap.Reason));
    }

    [Fact]
    public async Task IncompatibleBankRevisionFailsClosedInsteadOfReinterpretingSession()
    {
        var clock = new FixedTimeProvider(Start);
        await using var environment = await PersistenceTestEnvironment.CreateAsync(timeProvider: clock);
        using var source = DiagnosticBankTests.CreateSource(DiagnosticBankTests.FindBankDirectory());
        using var coordinator = new DiagnosticSessionCoordinator();
        DiagnosticSessionService sessions = CreateSessionService(environment, source, coordinator, clock);
        DiagnosticSessionView active = await sessions.StartAsync(DiagnosticMode.Reduced);
        DiagnosticSessionView abandoned = await sessions.AbandonAsync(active.Id);
        DiagnosticScoringRubric realRubric = await source.GetRubricAsync();
        var incompatible = realRubric with
        {
            Snapshot = realRubric.Snapshot with { BankRevision = new string('0', 64) },
        };
        DiagnosticEvaluationService evaluations = CreateEvaluationService(
            environment,
            new FixedRubricSource(incompatible),
            coordinator,
            clock);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            evaluations.GetOrCreateAsync(abandoned.Id).AsTask());

        Assert.Contains("révision figée", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<DiagnosticSessionView> CompleteReducedAsync(
        DiagnosticSessionService sessions,
        FileSystemDiagnosticBankSource source)
    {
        DiagnosticScoringRubric rubric = await source.GetRubricAsync();
        DiagnosticSessionView session = await sessions.StartAsync(DiagnosticMode.Reduced);
        for (int sectionIndex = 0; sectionIndex < session.Sections.Count; sectionIndex++)
        {
            session = await sessions.GetAsync(session.Id);
            foreach (DiagnosticQuestionView question in session.CurrentSection!.Questions)
            {
                session = await sessions.SaveResponseAsync(
                    session.Id,
                    question.Id,
                    rubric.ExpectedOptions[question.Id]);
            }

            session = await sessions.CompleteSectionAsync(session.Id, sectionIndex);
            if (sectionIndex < session.Sections.Count - 1)
            {
                session = await sessions.StartCurrentSectionAsync(session.Id);
            }
        }

        return await sessions.FinishAsync(session.Id);
    }

    private static DiagnosticSessionService CreateSessionService(
        PersistenceTestEnvironment environment,
        FileSystemDiagnosticBankSource source,
        DiagnosticSessionCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IDiagnosticSessionRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            new DiagnosticSessionOptions(),
            clock);

    private static DiagnosticEvaluationService CreateEvaluationService(
        PersistenceTestEnvironment environment,
        IDiagnosticRubricSource source,
        DiagnosticSessionCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IDiagnosticSessionRepository>(),
            environment.GetRequiredService<IDiagnosticEvaluationRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            coordinator,
            clock);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FixedRubricSource(DiagnosticScoringRubric rubric) : IDiagnosticRubricSource
    {
        public ValueTask<DiagnosticScoringRubric> GetRubricAsync(
            CancellationToken cancellationToken = default) => ValueTask.FromResult(rubric);
    }

    private sealed class ThrowingRubricSource : IDiagnosticRubricSource
    {
        public ValueTask<DiagnosticScoringRubric> GetRubricAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Un rapport persistant ne doit pas recharger le barème courant.");
    }
}
