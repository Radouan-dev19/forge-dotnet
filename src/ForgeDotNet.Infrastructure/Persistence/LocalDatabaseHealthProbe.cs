using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Persistence;

public sealed record LocalDatabaseHealth(bool IsHealthy, string Description, Exception? Exception = null);

public sealed class LocalDatabaseHealthProbe(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate)
{
    public async Task<LocalDatabaseHealth> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var lease = await databaseGate.AcquireAsync(cancellationToken);
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            if (!await context.Database.CanConnectAsync(cancellationToken))
            {
                return new LocalDatabaseHealth(false, "La base SQLite locale est inaccessible.");
            }

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                return new LocalDatabaseHealth(false, "La base SQLite locale possède une migration en attente.");
            }

            await context.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "PRAGMA quick_check;";
                var integrity = Convert.ToString(
                    await command.ExecuteScalarAsync(cancellationToken),
                    CultureInfo.InvariantCulture);
                if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    return new LocalDatabaseHealth(false, "Le contrôle d'intégrité SQLite a échoué.");
                }
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }

            return new LocalDatabaseHealth(true, "La base SQLite locale répond et son schéma est à jour.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new LocalDatabaseHealth(false, "La base SQLite locale ne répond pas.", exception);
        }
    }
}
