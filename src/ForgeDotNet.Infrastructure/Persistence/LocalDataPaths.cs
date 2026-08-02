using ForgeDotNet.Application.IdentityLocal;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class LocalDataPaths : ILocalDataLocation
{
    private const string ProductDirectoryName = "Forge.NET";

    private LocalDataPaths(string dataDirectory, string databasePath)
    {
        DataDirectory = dataDirectory;
        DatabasePath = databasePath;
    }

    public string DataDirectory { get; }

    public string DatabasePath { get; }

    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        ForeignKeys = true,
        Pooling = true,
        DefaultTimeout = 5,
    }.ToString();

    public static LocalDataPaths Create(LocalDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DatabaseFileName)
            || options.DatabaseFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || !string.Equals(Path.GetFileName(options.DatabaseFileName), options.DatabaseFileName, StringComparison.Ordinal)
            || !string.Equals(Path.GetExtension(options.DatabaseFileName), ".db", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("LocalData:DatabaseFileName doit être un nom de fichier .db sans chemin.");
        }

        var configuredDirectory = options.DirectoryPath;
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ProductDirectoryName,
                "data")
            : configuredDirectory;

        if (!Path.IsPathFullyQualified(directory))
        {
            throw new InvalidOperationException("LocalData:DirectoryPath doit être un chemin absolu.");
        }

        var canonicalDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var databasePath = Path.GetFullPath(Path.Combine(canonicalDirectory, options.DatabaseFileName));
        EnsureContained(databasePath, canonicalDirectory);

        return new LocalDataPaths(canonicalDirectory, databasePath);
    }

    public void EnsureOutside(params string?[] forbiddenDirectories)
    {
        foreach (var forbiddenDirectory in forbiddenDirectories)
        {
            if (string.IsNullOrWhiteSpace(forbiddenDirectory))
            {
                continue;
            }

            var canonicalForbiddenDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(forbiddenDirectory));
            if (IsContained(DatabasePath, canonicalForbiddenDirectory))
            {
                throw new InvalidOperationException(
                    "La base SQLite doit être stockée hors du répertoire de l'application et de son répertoire public.");
            }
        }
    }

    public string GetSuggestedBackupPath(DateTimeOffset createdAtUtc)
    {
        var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var backupDirectory = Path.Combine(documentsDirectory, ProductDirectoryName, "backups");
        var fileName = $"forge-dotnet-{createdAtUtc.UtcDateTime:yyyyMMdd-HHmmss}.backup.zip";
        return Path.Combine(backupDirectory, fileName);
    }

    internal static void EnsureContained(string candidatePath, string allowedDirectory)
    {
        if (!IsContained(candidatePath, allowedDirectory))
        {
            throw new InvalidOperationException("Le chemin résolu sort du répertoire autorisé.");
        }
    }

    private static bool IsContained(string candidatePath, string directoryPath)
    {
        var relativePath = Path.GetRelativePath(directoryPath, candidatePath);
        return !Path.IsPathFullyQualified(relativePath)
            && !relativePath.Equals("..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
