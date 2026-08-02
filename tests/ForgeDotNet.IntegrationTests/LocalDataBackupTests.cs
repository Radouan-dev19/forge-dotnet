using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;
using Microsoft.Data.Sqlite;

namespace ForgeDotNet.IntegrationTests;

public sealed class LocalDataBackupTests
{
    [Fact]
    public async Task BackupAndRestoreRoundTripPreservesTheProfile()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var repository = environment.GetRequiredService<ILocalProfileRepository>();
        var profile = (await repository.GetAsync())
            .Update("Avant sauvegarde", "Objectif durable", 14, InterfaceLanguage.French)
            .SetLearningContractAcceptance(accepted: true);
        await repository.SaveAsync(profile);
        var archivePath = GetArchivePath(environment, "roundtrip");
        var backupService = environment.GetRequiredService<ILocalDataBackupService>();

        var backup = await backupService.CreateBackupAsync(archivePath);
        await repository.SaveAsync(profile.Update("Après sauvegarde", "Objectif modifié", 8, InterfaceLanguage.French));
        var restore = await backupService.RestoreAsync(archivePath);
        var restored = await repository.GetAsync();

        Assert.Equal(archivePath, backup.ArchivePath);
        Assert.Equal(64, backup.ChecksumSha256.Length);
        Assert.Empty(Directory.GetFiles(environment.DataDirectory, "*.tmp", SearchOption.AllDirectories));
        Assert.True(File.Exists(restore.RecoveryDatabasePath));
        Assert.Equal("Avant sauvegarde", restored.DisplayName);
        Assert.Equal("Objectif durable", restored.ProfessionalGoal);
        Assert.Equal(14, restored.WeeklyAvailableHours);
        Assert.True(restored.HasAcceptedLearningContract);
    }

    [Fact]
    public async Task RestoreRejectsAnInvalidArchive()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var invalidArchive = GetArchivePath(environment, "invalid");
        await File.WriteAllTextAsync(invalidArchive, "ceci n'est pas une archive");

        await Assert.ThrowsAnyAsync<InvalidDataException>(() =>
            environment.GetRequiredService<ILocalDataBackupService>().RestoreAsync(invalidArchive));
    }

    [Fact]
    public async Task RestoreRejectsTraversalEntry()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var archivePath = await CreateValidBackupAsync(environment, "traversal");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
        {
            var traversal = archive.CreateEntry("../outside.txt");
            await using var stream = traversal.Open();
            await stream.WriteAsync("interdit"u8.ToArray());
        }

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            environment.GetRequiredService<ILocalDataBackupService>().RestoreAsync(archivePath));
        Assert.False(File.Exists(Path.Combine(environment.DataDirectory, "..", "outside.txt")));
    }

    [Fact]
    public async Task RestoreRejectsChecksumMismatch()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var archivePath = await CreateValidBackupAsync(environment, "checksum");
        await RewriteArchiveAsync(archivePath, databaseBytes =>
        {
            databaseBytes[^1] ^= 0xFF;
            return (databaseBytes, (Func<JsonObject, JsonObject>?)null);
        });

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            environment.GetRequiredService<ILocalDataBackupService>().RestoreAsync(archivePath));
        Assert.Contains("checksum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RestoreRejectsCorruptionEvenWithMatchingChecksum()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var archivePath = await CreateValidBackupAsync(environment, "corruption");
        await RewriteArchiveAsync(archivePath, _ =>
        {
            var corruptBytes = RandomNumberGenerator.GetBytes(256);
            return (corruptBytes, manifest =>
            {
                manifest["checksumSha256"] = Convert.ToHexString(SHA256.HashData(corruptBytes)).ToLowerInvariant();
                return manifest;
            }
            );
        });

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            environment.GetRequiredService<ILocalDataBackupService>().RestoreAsync(archivePath));
    }

    [Fact]
    public async Task RestoreRejectsAnUnsupportedManifestVersion()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        var archivePath = await CreateValidBackupAsync(environment, "version");
        await RewriteArchiveAsync(archivePath, databaseBytes =>
            (databaseBytes, manifest =>
            {
                manifest["formatVersion"] = 999;
                return manifest;
            }
        ));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            environment.GetRequiredService<ILocalDataBackupService>().RestoreAsync(archivePath));
    }

    private static async Task<string> CreateValidBackupAsync(
        PersistenceTestEnvironment environment,
        string name)
    {
        _ = await environment.GetRequiredService<ILocalProfileRepository>().GetAsync();
        var path = GetArchivePath(environment, name);
        await environment.GetRequiredService<ILocalDataBackupService>().CreateBackupAsync(path);
        SqliteConnection.ClearAllPools();
        return path;
    }

    private static string GetArchivePath(PersistenceTestEnvironment environment, string name) =>
        Path.Combine(environment.DataDirectory, $"{name}.backup.zip");

    private static async Task RewriteArchiveAsync(
        string archivePath,
        Func<byte[], (byte[] DatabaseBytes, Func<JsonObject, JsonObject>? ManifestTransform)> transform)
    {
        byte[] databaseBytes;
        JsonObject manifest;
        using (var archive = ZipFile.OpenRead(archivePath))
        {
            await using var databaseStream = archive.GetEntry("database.sqlite3")!.Open();
            using var databaseBuffer = new MemoryStream();
            await databaseStream.CopyToAsync(databaseBuffer);
            databaseBytes = databaseBuffer.ToArray();

            await using var manifestStream = archive.GetEntry("manifest.json")!.Open();
            manifest = (await JsonNode.ParseAsync(manifestStream))!.AsObject();
        }

        var transformed = transform(databaseBytes);
        if (transformed.ManifestTransform is not null)
        {
            manifest = transformed.ManifestTransform(manifest);
        }

        File.Delete(archivePath);
        using var output = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        var databaseEntry = output.CreateEntry("database.sqlite3");
        await using (var stream = databaseEntry.Open())
        {
            await stream.WriteAsync(transformed.DatabaseBytes);
        }

        var manifestEntry = output.CreateEntry("manifest.json");
        await using var outputManifest = manifestEntry.Open();
        await JsonSerializer.SerializeAsync(outputManifest, manifest);
    }
}
