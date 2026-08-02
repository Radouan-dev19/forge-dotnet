using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.DebugLab;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class DebugLabPersistenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FullJournalCycleIsVersionedPersistsAndNeverStoresSubmittedSource()
    {
        string dataDirectory;
        var clock = new FixedTimeProvider(Start);
        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(deleteOnDispose: false, timeProvider: clock))
        using (DebugContentFixture content = await DebugContentFixture.CreateAsync())
        using (var coordinator = new DebugLabCoordinator())
        {
            dataDirectory = firstRun.DataDirectory;
            DebugLabService service = CreateService(firstRun, content.Source, coordinator, clock);
            DebugLabActivityView initial = await service.GetOrStartAsync("debug-null-reference-001");
            Assert.Equal(DebugLabState.InvestigationRequired, initial.State);
            Assert.Null(initial.ProtectedSolution);

            DebugLabActivityView investigated = await service.SaveInvestigationAsync(
                initial.ScenarioId, initial.Version, Investigation());
            Assert.Equal(DebugLabState.CorrectionReady, investigated.State);
            DebugLabActivityView prepared = await service.PrepareCorrectionAsync(
                investigated.ScenarioId, investigated.Version,
                new DebugCorrectionPreparationInput(
                    "Ajouter une garde null avant Trim tout en conservant la normalisation.",
                    "Tester explicitement null, la valeur absente, le blanc et un nom nominal."));
            string submittedSource = "public static class Submission { public static string FormatCustomerName(string value) => string.IsNullOrWhiteSpace(value) ? \"(inconnu)\" : value.Trim().ToUpperInvariant(); }";
            clock.Advance(TimeSpan.FromMinutes(1));
            DebugCorrectionRunResult run = await service.RunCorrectionAsync(
                prepared.ScenarioId, prepared.Version, submittedSource);
            Assert.Equal(CodeRunStatus.Succeeded, run.RunnerResult.Status);
            Assert.Equal(DebugLabState.RootCauseRequired, run.Activity.State);

            clock.Advance(TimeSpan.FromMinutes(1));
            DebugLabActivityView completed = await service.CompleteAsync(
                run.Activity.ScenarioId,
                run.Activity.Version,
                "La valeur null est déréférencée par Trim avant que le formatage ne puisse agir.",
                "Imposer en revue un test de valeur absente et conserver le cas null dans la non-régression.");
            Assert.Equal(DebugLabState.Completed, completed.State);
            Assert.All(completed.EvaluationResults, result => Assert.True(result.Passed));
            string markdown = await service.ExportJournalMarkdownAsync(completed.ScenarioId);
            Assert.Contains("## Cause", markdown, StringComparison.Ordinal);
            Assert.DoesNotContain(submittedSource, markdown, StringComparison.Ordinal);
            Assert.DoesNotContain("Hidden_", markdown, StringComparison.Ordinal);

            await using var connection = new SqliteConnection($"Data Source={firstRun.DatabasePath};Mode=ReadOnly");
            await connection.OpenAsync();
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM DebugLabActivities;"));
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT COUNT(*) FROM DebugCorrectionAttempts;"));
            Assert.Equal(0L, await ScalarAsync(connection, "SELECT COUNT(*) FROM pragma_table_info('DebugCorrectionAttempts') WHERE lower(name) LIKE '%source%' AND name <> 'SourceFingerprint';"));
            Assert.Equal(0L, await ScalarAsync(connection, "SELECT COUNT(*) FROM DebugCorrectionAttempts WHERE SourceFingerprint LIKE '%public static%';"));
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(dataDirectory, timeProvider: clock);
        using DebugContentFixture restoredContent = await DebugContentFixture.CreateAsync();
        using var restoredCoordinator = new DebugLabCoordinator();
        DebugLabService restoredService = CreateService(secondRun, restoredContent.Source, restoredCoordinator, clock);
        DebugLabActivityView restored = await restoredService.GetOrStartAsync("debug-null-reference-001");
        Assert.Equal(DebugLabState.Completed, restored.State);
        Assert.Single(restored.Attempts);
        Assert.All(restored.EvaluationResults, result => Assert.True(result.Passed));
    }

    private static DebugLabService CreateService(
        PersistenceTestEnvironment environment,
        IDebugScenarioSource source,
        DebugLabCoordinator coordinator,
        TimeProvider clock) => new(
            source,
            environment.GetRequiredService<IDebugLabRepository>(),
            environment.GetRequiredService<ILocalProfileRepository>(),
            new SuccessfulCodeRunner(clock),
            coordinator,
            clock);

    private static DebugInvestigationInput Investigation() => new(
        "Une valeur absente provoque une NullReferenceException reproductible.",
        "L'import appelle FormatCustomerName avec un nom client absent.",
        "Trim pourrait déréférencer la valeur null avant la normalisation.",
        "La pile et la Watch montrent une valeur null au moment de Trim.",
        "Arrêt placé sur l'appel à Trim.",
        "Watch value affiche null.",
        "Locals confirme le paramètre absent.",
        "Call Stack relie l'import à FormatCustomerName.");

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private sealed class SuccessfulCodeRunner(TimeProvider clock) : ICodeRunner
    {
        public ValueTask<CodeRunResult> RunAsync(CodeRunRequest request, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = clock.GetUtcNow();
            return ValueTask.FromResult(new CodeRunResult(
                request.RequestId,
                CodeRunStatus.Succeeded,
                new CodeCompilationResult(CodeRunStageStatus.Succeeded, [], new CodeRunTextOutput("Compilation réussie.", false)),
                new CodeTestResult(CodeRunStageStatus.Succeeded, 6, 6, 0, 0, false, [], new CodeRunTextOutput("Six tests réussis.", false)),
                "Correction validée dans le runner de test.",
                Guid.NewGuid(), now, now));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
