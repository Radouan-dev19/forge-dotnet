namespace ForgeDotNet.Application.IdentityLocal;

public interface ILocalDataBackupService
{
    Task<LocalDataBackupResult> CreateBackupAsync(
        string destinationArchivePath,
        CancellationToken cancellationToken = default);

    Task<LocalDataRestoreResult> RestoreAsync(
        string sourceArchivePath,
        CancellationToken cancellationToken = default);
}

public sealed record LocalDataBackupResult(
    string ArchivePath,
    string ChecksumSha256,
    string MigrationId,
    DateTimeOffset CreatedAtUtc);

public sealed record LocalDataRestoreResult(
    string ArchivePath,
    string RecoveryDatabasePath,
    string MigrationId,
    DateTimeOffset RestoredAtUtc);
