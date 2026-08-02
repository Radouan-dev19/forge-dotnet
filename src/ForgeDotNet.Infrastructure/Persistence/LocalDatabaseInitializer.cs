using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class LocalDatabaseInitializer(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate,
    LocalDataPaths paths)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.DataDirectory);

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL;";
            var journalMode = Convert.ToString(
                await command.ExecuteScalarAsync(cancellationToken),
                CultureInfo.InvariantCulture);
            if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
            {
                throw new SqliteException("SQLite n'a pas activé le mode WAL.", 1);
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
