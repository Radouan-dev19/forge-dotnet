using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.IdentityLocal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class LocalDataBackupService(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate,
    LocalDataPaths paths,
    TimeProvider timeProvider) : ILocalDataBackupService
{
    private const int BackupFormatVersion = 1;
    private const long MaximumDatabaseBytes = 512L * 1024 * 1024;
    private const long MaximumManifestBytes = 64L * 1024;
    private const string DatabaseEntryName = "database.sqlite3";
    private const string ManifestEntryName = "manifest.json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public async Task<LocalDataBackupResult> CreateBackupAsync(
        string destinationArchivePath,
        CancellationToken cancellationToken = default)
    {
        var archivePath = ResolveArchivePath(destinationArchivePath, mustExist: false);
        var archiveDirectory = Path.GetDirectoryName(archivePath)
            ?? throw new InvalidOperationException("Le répertoire de sauvegarde est introuvable.");
        Directory.CreateDirectory(archiveDirectory);

        var temporaryDatabasePath = Path.Combine(
            archiveDirectory,
            $".{Path.GetFileName(archivePath)}.{Guid.NewGuid():N}.db.tmp");
        var temporaryArchivePath = Path.Combine(
            archiveDirectory,
            $".{Path.GetFileName(archivePath)}.{Guid.NewGuid():N}.zip.tmp");

        try
        {
            await using var lease = await databaseGate.AcquireAsync(cancellationToken);
            if (!File.Exists(paths.DatabasePath))
            {
                throw new InvalidOperationException("La base locale n'existe pas encore.");
            }

            await CheckpointAsync(cancellationToken);
            await BackupDatabaseAsync(temporaryDatabasePath, cancellationToken);
            var checksum = await ComputeSha256Async(temporaryDatabasePath, cancellationToken);
            var migrationId = await GetCurrentMigrationIdAsync(cancellationToken);
            var createdAtUtc = timeProvider.GetUtcNow();
            var manifest = new BackupManifest(
                BackupFormatVersion,
                DatabaseEntryName,
                checksum,
                migrationId,
                createdAtUtc);

            await CreateArchiveAsync(temporaryArchivePath, temporaryDatabasePath, manifest, cancellationToken);
            AtomicReplaceFile(temporaryArchivePath, archivePath);

            return new LocalDataBackupResult(archivePath, checksum, migrationId, createdAtUtc);
        }
        finally
        {
            DeleteFileIfPresent(temporaryDatabasePath);
            DeleteFileIfPresent(temporaryArchivePath);
        }
    }

    public async Task<LocalDataRestoreResult> RestoreAsync(
        string sourceArchivePath,
        CancellationToken cancellationToken = default)
    {
        var archivePath = ResolveArchivePath(sourceArchivePath, mustExist: true);
        var stagingDirectory = Path.Combine(paths.DataDirectory, $".restore-{Guid.NewGuid():N}");
        LocalDataPaths.EnsureContained(stagingDirectory, paths.DataDirectory);
        Directory.CreateDirectory(stagingDirectory);
        var stagedDatabasePath = Path.Combine(stagingDirectory, DatabaseEntryName);
        var replacementSucceeded = false;
        string recoveryDatabasePath = string.Empty;

        try
        {
            await using var lease = await databaseGate.AcquireAsync(cancellationToken);
            var manifest = await ExtractAndValidateArchiveAsync(
                archivePath,
                stagedDatabasePath,
                cancellationToken);
            try
            {
                await ValidateDatabaseAsync(stagedDatabasePath, manifest.MigrationId, cancellationToken);
            }
            catch (SqliteException exception)
            {
                throw new InvalidDataException("La base sauvegardée est corrompue ou illisible.", exception);
            }

            Directory.CreateDirectory(paths.DataDirectory);
            if (File.Exists(paths.DatabasePath))
            {
                await CheckpointAsync(cancellationToken);
                SqliteConnection.ClearAllPools();
                DeleteSqliteSidecars();
                recoveryDatabasePath = Path.Combine(
                    paths.DataDirectory,
                    $"forge-dotnet.pre-restore-{timeProvider.GetUtcNow().UtcDateTime:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");
                File.Replace(stagedDatabasePath, paths.DatabasePath, recoveryDatabasePath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(stagedDatabasePath, paths.DatabasePath);
            }

            replacementSucceeded = true;
            await ValidateDatabaseAsync(paths.DatabasePath, manifest.MigrationId, cancellationToken);

            return new LocalDataRestoreResult(
                archivePath,
                recoveryDatabasePath,
                manifest.MigrationId,
                timeProvider.GetUtcNow());
        }
        catch
        {
            if (replacementSucceeded && !string.IsNullOrEmpty(recoveryDatabasePath) && File.Exists(recoveryDatabasePath))
            {
                SqliteConnection.ClearAllPools();
                File.Replace(recoveryDatabasePath, paths.DatabasePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                LocalDataPaths.EnsureContained(stagingDirectory, paths.DataDirectory);
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    private async Task CheckpointAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(paths.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task BackupDatabaseAsync(string destinationPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var source = new SqliteConnection(paths.ConnectionString);
        await source.OpenAsync(cancellationToken);
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString());
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
    }

    private async Task<string> GetCurrentMigrationIdAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        return appliedMigrations.LastOrDefault()
            ?? throw new InvalidOperationException("Aucune migration n'est appliquée à la base locale.");
    }

    private static async Task CreateArchiveAsync(
        string archivePath,
        string databasePath,
        BackupManifest manifest,
        CancellationToken cancellationToken)
    {
        await using var archiveStream = new FileStream(
            archivePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true);

        var databaseEntry = archive.CreateEntry(DatabaseEntryName, CompressionLevel.Optimal);
        await using (var entryStream = databaseEntry.Open())
        await using (var databaseStream = File.OpenRead(databasePath))
        {
            await databaseStream.CopyToAsync(entryStream, cancellationToken);
        }

        var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
        await using var manifestStream = manifestEntry.Open();
        await JsonSerializer.SerializeAsync(manifestStream, manifest, SerializerOptions, cancellationToken);
    }

    private static async Task<BackupManifest> ExtractAndValidateArchiveAsync(
        string archivePath,
        string stagedDatabasePath,
        CancellationToken cancellationToken)
    {
        await using var archiveStream = new FileStream(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);

        if (archive.Entries.Count != 2
            || archive.Entries.Any(entry => !string.Equals(entry.FullName, entry.Name, StringComparison.Ordinal)))
        {
            throw new InvalidDataException("L'archive contient des entrées non autorisées.");
        }

        var databaseEntry = archive.GetEntry(DatabaseEntryName)
            ?? throw new InvalidDataException("La base SQLite manque dans l'archive.");
        var manifestEntry = archive.GetEntry(ManifestEntryName)
            ?? throw new InvalidDataException("Le manifeste manque dans l'archive.");

        ValidateEntrySize(databaseEntry, MaximumDatabaseBytes);
        ValidateEntrySize(manifestEntry, MaximumManifestBytes);

        BackupManifest manifest;
        await using (var manifestStream = manifestEntry.Open())
        {
            manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(
                manifestStream,
                SerializerOptions,
                cancellationToken)
                ?? throw new InvalidDataException("Le manifeste est vide.");
        }

        if (manifest.FormatVersion != BackupFormatVersion
            || !string.Equals(manifest.DatabaseEntry, DatabaseEntryName, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(manifest.MigrationId)
            || manifest.CreatedAtUtc.Offset != TimeSpan.Zero
            || manifest.ChecksumSha256.Length != 64)
        {
            throw new InvalidDataException("Le manifeste de sauvegarde est invalide ou incompatible.");
        }

        await using (var source = databaseEntry.Open())
        await using (var destination = new FileStream(
            stagedDatabasePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await CopyWithLimitAsync(source, destination, MaximumDatabaseBytes, cancellationToken);
        }

        var checksum = await ComputeSha256Async(stagedDatabasePath, cancellationToken);
        if (!string.Equals(checksum, manifest.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Le checksum de la sauvegarde est invalide.");
        }

        return manifest;
    }

    private static async Task ValidateDatabaseAsync(
        string databasePath,
        string expectedMigrationId,
        CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            ForeignKeys = true,
        }.ToString();
        var options = new DbContextOptionsBuilder<ForgeDbContext>()
            .UseSqlite(connectionString)
            .Options;
        await using var context = new ForgeDbContext(options);
        await context.Database.OpenConnectionAsync(cancellationToken);

        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";
            var integrity = Convert.ToString(
                await command.ExecuteScalarAsync(cancellationToken),
                CultureInfo.InvariantCulture);
            if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("La base sauvegardée est corrompue.");
            }
        }

        var knownMigrations = context.Database.GetMigrations().ToArray();
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        if (knownMigrations.Length == 0
            || !knownMigrations.SequenceEqual(appliedMigrations, StringComparer.Ordinal)
            || !string.Equals(appliedMigrations[^1], expectedMigrationId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Le schéma de la sauvegarde est incompatible avec cette version.");
        }

        var profiles = await context.LocalProfiles.AsNoTracking().Take(2).ToListAsync(cancellationToken);
        if (profiles.Count > 1)
        {
            throw new InvalidDataException("La sauvegarde contient plusieurs profils locaux.");
        }

        _ = profiles.SingleOrDefault()?.ToDomain();
    }

    private static string ResolveArchivePath(string archivePath, bool mustExist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        if (!Path.IsPathFullyQualified(archivePath))
        {
            throw new InvalidOperationException("Le chemin de sauvegarde doit être absolu.");
        }

        var canonicalPath = Path.GetFullPath(archivePath);
        if (!canonicalPath.EndsWith(".backup.zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Le fichier doit porter l'extension .backup.zip.");
        }

        if (mustExist && !File.Exists(canonicalPath))
        {
            throw new FileNotFoundException("La sauvegarde demandée est introuvable.", canonicalPath);
        }

        return canonicalPath;
    }

    private static void ValidateEntrySize(ZipArchiveEntry entry, long maximumBytes)
    {
        if (entry.Length <= 0 || entry.Length > maximumBytes)
        {
            throw new InvalidDataException($"L'entrée '{entry.Name}' a une taille invalide.");
        }
    }

    private static async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long totalBytes = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > maximumBytes)
            {
                throw new InvalidDataException("L'archive dépasse la taille autorisée.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var checksum = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(checksum).ToLowerInvariant();
    }

    private static void AtomicReplaceFile(string temporaryPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(temporaryPath, destinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(temporaryPath, destinationPath);
        }
    }

    private void DeleteSqliteSidecars()
    {
        DeleteFileIfPresent($"{paths.DatabasePath}-wal");
        DeleteFileIfPresent($"{paths.DatabasePath}-shm");
    }

    private static void DeleteFileIfPresent(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private sealed record BackupManifest(
        int FormatVersion,
        string DatabaseEntry,
        string ChecksumSha256,
        string MigrationId,
        DateTimeOffset CreatedAtUtc);
}
