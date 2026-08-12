using System.Globalization;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Domain.SqlLab;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class MasteryPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TypedSourcesSnapshotsAndPolicyRemainImmutableAcrossRestart()
    {
        string dataDirectory;
        var clock = new FixedTimeProvider(Now);
        Guid profileId;
        string firstRevision;

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(
            deleteOnDispose: false,
            timeProvider: clock))
        {
            dataDirectory = firstRun.DataDirectory;
            ILocalProfileRepository profiles = firstRun.GetRequiredService<ILocalProfileRepository>();
            UserProfile profile = await profiles.GetAsync();
            profileId = profile.LocalId;
            await SeedPracticeAndDebugAsync(firstRun.DatabasePath, profileId);

            IPracticeLearningAttemptRepository practiceAttempts =
                firstRun.GetRequiredService<IPracticeLearningAttemptRepository>();
            var runner = new RunExercise(
                new RecordingCodeRunner(clock),
                new RunExerciseHistory(),
                clock,
                practiceAttempts,
                profiles);
            const string source = "public static class Submission { public static int Add(int a, int b) => a + b; } // raw-source-must-not-be-stored";
            _ = await runner.ExecuteAsync(new RunExerciseCommand(
                Guid.NewGuid(),
                "practice-mastery-source",
                1,
                new string('e', 64),
                [new CodeRunSourceFile("Submission.cs", source)]));
            IReadOnlyList<PracticeLearningAttempt> storedPractice = await practiceAttempts.ListAsync(profileId);
            PracticeLearningAttempt storedPracticeAttempt = Assert.Single(storedPractice);
            Assert.StartsWith("sha256:", storedPracticeAttempt.SubmissionFingerprint, StringComparison.Ordinal);
            Assert.DoesNotContain("Submission", storedPracticeAttempt.SubmissionFingerprint, StringComparison.Ordinal);
            await AssertNoRawPracticeSourceAsync(firstRun.DatabasePath, source);
            await Assert.ThrowsAsync<InvalidOperationException>(() => practiceAttempts.AppendAsync(
                storedPracticeAttempt with { SubmissionFingerprint = new string('a', 64) }).AsTask());

            ISqlLearningAttemptRepository sqlAttempts = firstRun.GetRequiredService<ISqlLearningAttemptRepository>();
            var gateway = new RecordingSqlGateway();
            // Aucun scénario publié n'est fourni : la session reste le bac à sable technique.
            var sqlLab = new SqlLabService(gateway, null, sqlAttempts, profiles, clock);
            const string query = "SELECT OrderId, CustomerName, Total FROM dbo.Orders ORDER BY OrderId; /* raw-query-must-not-be-stored */";
            SqlLabRunView run = await sqlLab.ExecuteAsync(Guid.NewGuid(), query, validateReference: true);
            Assert.True(run.Validation?.Passed);

            IReadOnlyList<SqlLearningAttempt> storedSql = await sqlAttempts.ListAsync(profileId);
            SqlLearningAttempt storedAttempt = Assert.Single(storedSql);
            Assert.Equal(64, storedAttempt.QueryFingerprint.Length);
            Assert.DoesNotContain("SELECT", storedAttempt.QueryFingerprint, StringComparison.OrdinalIgnoreCase);
            Assert.True(storedAttempt.ValidationPassed);
            await AssertNoRawSqlColumnOrValueAsync(firstRun.DatabasePath, query);

            SqlLearningAttempt replay = storedAttempt with { Id = Guid.NewGuid(), QueryFingerprint = new string('a', 64) };
            await Assert.ThrowsAsync<InvalidOperationException>(() => sqlAttempts.AppendAsync(replay).AsTask());

            IMasteryEvidenceSource evidenceSource = firstRun.GetRequiredService<IMasteryEvidenceSource>();
            MasteryEvidenceSet evidence = await evidenceSource.ReadAsync(profileId);
            Assert.Equal(4, evidence.Observations.Count);
            MasteryObservation[] practice = evidence.Observations
                .Where(item => item.Source == MasteryEvidenceSource.Practice)
                .ToArray();
            MasteryObservation debug = Assert.Single(evidence.Observations, item => item.Source == MasteryEvidenceSource.DebugLab);
            MasteryObservation sql = Assert.Single(evidence.Observations, item => item.Source == MasteryEvidenceSource.SqlLab);
            Assert.Contains(practice, item => item.Verification == MasteryVerificationKind.ManualDeclaration);
            Assert.Contains(practice, item => item.Verification == MasteryVerificationKind.AutomaticTests);
            Assert.All(practice, item => Assert.Equal(MasteryAssistance.Hint2, item.Assistance));
            Assert.Equal(MasteryVerificationKind.AutomaticTests, debug.Verification);
            Assert.Equal(MasteryVerificationKind.AutomaticTests, sql.Verification);

            var mastery = CreateService(firstRun, profiles, clock);
            MasteryDashboardView[] concurrent = await Task.WhenAll(
                Enumerable.Range(0, 4).Select(_ => mastery.GetAsync().AsTask()));
            Assert.Single(concurrent.Select(item => item.EvidenceRevision).Distinct(StringComparer.Ordinal));
            firstRevision = concurrent[0].EvidenceRevision;
            Assert.Equal(1L, await ScalarAsync(firstRun.DatabasePath, "SELECT COUNT(*) FROM MasteryProjections;"));
            Assert.Contains(
                "AutonomousPractice",
                await TextScalarAsync(firstRun.DatabasePath, "SELECT FrozenPolicyJson FROM MasteryProjections;"),
                StringComparison.Ordinal);

            await sqlAttempts.AppendAsync(CreateSqlAttempt(profileId, Guid.NewGuid(), Now.AddMinutes(1)));
            MasteryDashboardView changed = await mastery.GetAsync();
            Assert.NotEqual(firstRevision, changed.EvidenceRevision);
            Assert.Equal(2L, await ScalarAsync(firstRun.DatabasePath, "SELECT COUNT(*) FROM MasteryProjections;"));
            Assert.All(changed.Gates, gate => Assert.False(gate.IsOpen));
            Assert.Equal(45m, changed.Domains.Single(item => item.Domain == MasteryDomain.Sql).Score);
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(
            dataDirectory,
            timeProvider: clock);
        ILocalProfileRepository restoredProfiles = secondRun.GetRequiredService<ILocalProfileRepository>();
        MasteryDashboardView restored = await CreateService(secondRun, restoredProfiles, clock).GetAsync();
        Assert.Equal(2L, await ScalarAsync(secondRun.DatabasePath, "SELECT COUNT(*) FROM MasteryProjections;"));
        Assert.NotEqual(firstRevision, restored.EvidenceRevision);
        Assert.Equal(profileId, (await restoredProfiles.GetAsync()).LocalId);
    }

    private static MasteryService CreateService(
        PersistenceTestEnvironment environment,
        ILocalProfileRepository profiles,
        TimeProvider clock) => new(
            profiles,
            environment.GetRequiredService<IMasteryEvidenceSource>(),
            environment.GetRequiredService<IMasteryProjectionRepository>(),
            environment.GetRequiredService<IMasteryPolicySource>(),
            clock);

    private static SqlLearningAttempt CreateSqlAttempt(
        Guid profileId,
        Guid diagnosticId,
        DateTimeOffset observedAtUtc) => SqlLearningAttempt.Create(
            profileId,
            "sql-lab-reference-001",
            1,
            "sql-lab-reference-v1",
            new SqlLabExecutionResult(
                SqlLabExecutionStatus.Succeeded,
                new SqlLabResultSet([], []),
                [],
                new SqlLabValidationResult(true, []),
                "Résultat conforme.",
                diagnosticId,
                TimeSpan.FromMilliseconds(12)),
            validationRequested: true,
            new string('b', 64),
            observedAtUtc);

    private static async Task SeedPracticeAndDebugAsync(string databasePath, Guid profileId)
    {
        Guid practiceActivityId = Guid.NewGuid();
        Guid debugActivityId = Guid.NewGuid();
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await ExecuteAsync(connection, """
            INSERT INTO PracticeActivities
                (Id, ProfileId, ExerciseId, ExerciseVersion, ContentRevision, Version, State, StartedAtUtc)
            VALUES ($practiceActivityId, $profileId, 'practice-mastery-source', 1, $practiceRevision, 3, 'Attempting', $now);
            INSERT INTO PracticeAttempts
                (Id, ActivityId, Sequence, SubmissionText, ManualVerificationNotes, ManualCheckDeclared,
                 IsSerious, Decision, SubmissionFingerprint, SubmittedAtUtc)
            VALUES ($practiceAttemptId, $practiceActivityId, 1, 'proposition manuelle suffisamment longue pour le test',
                    'vérification manuelle suffisamment détaillée', 1, 1, 'Serious', $practiceFingerprint, $now);
            INSERT INTO PracticeHintUsages (Id, ActivityId, Level, Kind, UsedAtUtc)
            VALUES ($hintId, $practiceActivityId, 2, 'location', $now);

            INSERT INTO DebugLabActivities
                (Id, ProfileId, ScenarioId, ScenarioVersion, ContentRevision, Version, State, StartedAtUtc,
                 Symptom, Context, Hypotheses, Evidence, Cause, Fix, Test, Prevention, CompletedAtUtc)
            VALUES ($debugActivityId, $profileId, 'debug-mastery-source', 1, 'sha256:debug', 5, 'Completed', $now,
                    'symptôme observé', 'contexte observé', 'hypothèse observée', 'preuve observée',
                    'cause observée', 'correction observée', 'test observé', 'prévention observée', $now);
            INSERT INTO DebugCorrectionAttempts
                (Id, ActivityId, Sequence, SourceFingerprint, Outcome, TotalTests, PassedTests, FailedTests,
                 DiagnosticId, SubmittedAtUtc)
            VALUES ($debugAttemptId, $debugActivityId, 1, $debugFingerprint, 'Succeeded', 3, 3, 0,
                    $debugDiagnosticId, $now);
            """,
            new Dictionary<string, object>
            {
                ["$practiceActivityId"] = practiceActivityId,
                ["$practiceAttemptId"] = Guid.NewGuid(),
                ["$hintId"] = Guid.NewGuid(),
                ["$debugActivityId"] = debugActivityId,
                ["$debugAttemptId"] = Guid.NewGuid(),
                ["$debugDiagnosticId"] = Guid.NewGuid(),
                ["$profileId"] = profileId,
                ["$practiceFingerprint"] = new string('c', 64),
                ["$practiceRevision"] = new string('e', 64),
                ["$debugFingerprint"] = new string('d', 64),
                ["$now"] = Now.AddMinutes(-1).ToString("O", CultureInfo.InvariantCulture),
            });
    }

    private static async Task AssertNoRawPracticeSourceAsync(string databasePath, string rawSource)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var columns = connection.CreateCommand();
        columns.CommandText = "SELECT name FROM pragma_table_info('PracticeLearningAttempts') ORDER BY cid;";
        var names = new List<string>();
        await using (SqliteDataReader reader = await columns.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        }

        Assert.DoesNotContain(names, name => name.Contains("Source", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Submission", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name, "SubmissionFingerprint", StringComparison.Ordinal));
        await using var values = connection.CreateCommand();
        values.CommandText = "SELECT * FROM PracticeLearningAttempts;";
        await using SqliteDataReader valueReader = await values.ExecuteReaderAsync();
        Assert.True(await valueReader.ReadAsync());
        for (int index = 0; index < valueReader.FieldCount; index++)
        {
            string stored = Convert.ToString(valueReader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;
            Assert.DoesNotContain(rawSource, stored, StringComparison.Ordinal);
            Assert.DoesNotContain("raw-source-must-not-be-stored", stored, StringComparison.Ordinal);
        }
    }

    private static async Task AssertNoRawSqlColumnOrValueAsync(string databasePath, string rawQuery)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var columns = connection.CreateCommand();
        columns.CommandText = "SELECT name FROM pragma_table_info('SqlLearningAttempts') ORDER BY cid;";
        var names = new List<string>();
        await using (SqliteDataReader reader = await columns.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        }

        Assert.DoesNotContain(names, name => name.Contains("Query", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(name, "QueryFingerprint", StringComparison.Ordinal));
        await using var values = connection.CreateCommand();
        values.CommandText = "SELECT * FROM SqlLearningAttempts;";
        await using SqliteDataReader valueReader = await values.ExecuteReaderAsync();
        Assert.True(await valueReader.ReadAsync());
        for (int index = 0; index < valueReader.FieldCount; index++)
        {
            string stored = Convert.ToString(valueReader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;
            Assert.DoesNotContain(rawQuery, stored, StringComparison.Ordinal);
            Assert.DoesNotContain("raw-query-must-not-be-stored", stored, StringComparison.Ordinal);
        }
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        IReadOnlyDictionary<string, object> parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach ((string key, object value) in parameters)
        {
            command.Parameters.AddWithValue(key, value);
        }

        _ = await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<string> TextScalarAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private sealed class RecordingSqlGateway : ISqlLabGateway
    {
        public Task<SqlLabExecutionResult> ExecuteAsync(
            Guid sessionId,
            string query,
            SqlLabExpectedResult? expectation,
            CancellationToken cancellationToken = default) => Task.FromResult(new SqlLabExecutionResult(
                SqlLabExecutionStatus.Succeeded,
                new SqlLabResultSet(
                    [
                        new SqlLabColumn("OrderId", "int", false),
                        new SqlLabColumn("CustomerName", "nvarchar", false),
                        new SqlLabColumn("Total", "decimal", false),
                    ],
                    [
                        [new("1"), new("Ada"), new("120.50")],
                        [new("2"), new("Grace"), new("75.00")],
                        [new("3"), new("Linus"), new("40.25")],
                    ]),
                [],
                expectation is null ? null : new SqlLabValidationResult(true, []),
                "Résultat conforme.",
                Guid.NewGuid(),
                TimeSpan.FromMilliseconds(10)));

        public Task<SqlLabAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqlLabAvailability(true, "Disponible"));

        public Task<SqlLabSessionDescriptor> CreateSessionAsync(
            SqlLabProvisioning? provisioning = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<SqlLabSessionDescriptor> ResetSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RecordingCodeRunner(TimeProvider clock) : ICodeRunner
    {
        public ValueTask<CodeRunResult> RunAsync(
            CodeRunRequest request,
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = clock.GetUtcNow();
            return ValueTask.FromResult(new CodeRunResult(
                request.RequestId,
                CodeRunStatus.Succeeded,
                new CodeCompilationResult(
                    CodeRunStageStatus.Succeeded,
                    [],
                    new CodeRunTextOutput(string.Empty, false)),
                new CodeTestResult(
                    CodeRunStageStatus.Succeeded,
                    3,
                    3,
                    0,
                    0,
                    false,
                    [],
                    new CodeRunTextOutput(string.Empty, false)),
                "Succès",
                Guid.NewGuid(),
                now.AddSeconds(-1),
                now));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
