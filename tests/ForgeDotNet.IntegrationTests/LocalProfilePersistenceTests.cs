using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.IntegrationTests;

public sealed class LocalProfilePersistenceTests
{
    [Fact]
    public async Task ProfileSurvivesACompleteServiceProviderRestart()
    {
        string dataDirectory;
        Guid localId;

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(deleteOnDispose: false))
        {
            dataDirectory = firstRun.DataDirectory;
            var repository = firstRun.GetRequiredService<ILocalProfileRepository>();
            var original = await repository.GetAsync();
            localId = original.LocalId;
            var updated = original
                .Update("Radouan", "Développeur backend .NET", 12, InterfaceLanguage.French)
                .SetLearningContractAcceptance(accepted: true);
            await repository.SaveAsync(updated);

            SqliteConnection.ClearAllPools();
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(dataDirectory);
        var persisted = await secondRun.GetRequiredService<ILocalProfileRepository>().GetAsync();

        Assert.Equal(localId, persisted.LocalId);
        Assert.Equal("Radouan", persisted.DisplayName);
        Assert.Equal("Développeur backend .NET", persisted.ProfessionalGoal);
        Assert.Equal(12, persisted.WeeklyAvailableHours);
        Assert.True(persisted.HasAcceptedLearningContract);
    }

    [Fact]
    public async Task InitialMigrationIsIdempotentOnAnEmptyDatabase()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var initializer = environment.GetRequiredService<LocalDatabaseInitializer>();

        await initializer.MigrateAsync();
        await initializer.MigrateAsync();

        var factory = environment.GetRequiredService<IDbContextFactory<ForgeDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();

        Assert.Equal(11, applied.Length);
        Assert.EndsWith("_InitialLocalProfile", applied[0], StringComparison.Ordinal);
        Assert.EndsWith("_LessonReaderState", applied[1], StringComparison.Ordinal);
        Assert.EndsWith("_DiagnosticSessions", applied[2], StringComparison.Ordinal);
        Assert.EndsWith("_DiagnosticEvaluations", applied[3], StringComparison.Ordinal);
        Assert.EndsWith("_WeeklyPlans", applied[4], StringComparison.Ordinal);
        Assert.EndsWith("_PracticeProtocol", applied[5], StringComparison.Ordinal);
        Assert.EndsWith("_DebugLab", applied[6], StringComparison.Ordinal);
        Assert.EndsWith("_Mastery", applied[7], StringComparison.Ordinal);
        Assert.EndsWith("_PracticeLearningObservations", applied[8], StringComparison.Ordinal);
        Assert.EndsWith("_ReviewScheduling", applied[9], StringComparison.Ordinal);
        Assert.EndsWith("_ExamsDashboard", applied[10], StringComparison.Ordinal);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task InitialMigrationCanRollbackAndApplyAgainOnATemporaryDatabase()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var factory = environment.GetRequiredService<IDbContextFactory<ForgeDbContext>>();
        await using var context = await factory.CreateDbContextAsync();

        await context.Database.MigrateAsync("0");
        await using (var connection = new SqliteConnection($"Data Source={environment.DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='LocalProfiles';";
            Assert.Equal(0L, Convert.ToInt64(
                await command.ExecuteScalarAsync(),
                System.Globalization.CultureInfo.InvariantCulture));
        }

        await context.Database.MigrateAsync();
        var profile = await environment.GetRequiredService<ILocalProfileRepository>().GetAsync();
        Assert.NotEqual(Guid.Empty, profile.LocalId);
    }

    [Fact]
    public async Task ConcurrentWritesRemainAValidSingleProfile()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var repository = environment.GetRequiredService<ILocalProfileRepository>();
        var initial = await repository.GetAsync();
        var writes = Enumerable.Range(1, 8)
            .Select(index => repository.SaveAsync(initial.Update(
                $"Profil {index}",
                $"Objectif {index}",
                index,
                InterfaceLanguage.French)).AsTask());

        await Task.WhenAll(writes);

        var stored = await repository.GetAsync();
        Assert.Equal(initial.LocalId, stored.LocalId);
        Assert.InRange(stored.WeeklyAvailableHours, 1, 8);

        await using var connection = new SqliteConnection($"Data Source={environment.DatabasePath};Mode=ReadOnly");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM LocalProfiles;";
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task HealthProbeChecksSchemaAndIntegrity()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        _ = await environment.GetRequiredService<ILocalProfileRepository>().GetAsync();

        var health = await environment.GetRequiredService<LocalDatabaseHealthProbe>().CheckAsync();

        Assert.True(health.IsHealthy, health.Description);
        Assert.Contains("schéma est à jour", health.Description, StringComparison.Ordinal);
    }
}
