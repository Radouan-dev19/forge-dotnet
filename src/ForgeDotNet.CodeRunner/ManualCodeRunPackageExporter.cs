using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.CodeRunner;

namespace ForgeDotNet.CodeRunner;

public sealed class ManualCodeRunPackageExporter : IManualCodeRunPackageExporter
{
    private const int MaximumPackageBytes = CodeRunContract.MaximumTotalSourceBytes + (32 * 1024);
    private static readonly DateTimeOffset StableTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly UTF8Encoding Utf8WithoutBom = new(false, true);
    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public ValueTask<ManualCodeRunPackage> ExportAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        CodeRunContract.ValidateRequest(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true, Utf8WithoutBom))
        {
            WriteEntry(
                archive,
                "README.md",
                "# Export manuel Forge.NET\n\n"
                + "Cette archive contient uniquement votre proposition et ses métadonnées publiques. "
                + "Elle ne contient aucun test caché, aucune solution et ne constitue aucune preuve automatique.\n\n"
                + "Travaillez dans un dossier jetable, inspectez le code avant exécution, puis créez un projet local contrôlé :\n\n"
                + "```powershell\n"
                + "dotnet new classlib --name ManualSubmission\n"
                + "Copy-Item -LiteralPath .\\sources\\*.cs -Destination .\\ManualSubmission\\\n"
                + "dotnet build .\\ManualSubmission\\ManualSubmission.csproj\n"
                + "```\n\n"
                + "Forge.NET n’observe pas cette exécution et n’enregistre aucun succès automatique.\n");

            string manifest = JsonSerializer.Serialize(
                new ManualManifest(
                    SchemaVersion: 1,
                    request.RequestId,
                    request.ExerciseId,
                    request.ExerciseVersion,
                    request.ContentRevision,
                    request.SourceFiles.Select(source => source.FileName).ToArray()),
                ManifestJsonOptions);
            WriteEntry(archive, "forge-manual.json", manifest);
            foreach (CodeRunSourceFile sourceFile in request.SourceFiles)
            {
                WriteEntry(archive, $"sources/{sourceFile.FileName}", sourceFile.Content);
            }
        }

        if (output.Length > MaximumPackageBytes)
        {
            throw new InvalidDataException("L’archive manuelle dépasse la limite autorisée.");
        }

        return ValueTask.FromResult(new ManualCodeRunPackage(
            $"forge-{request.ExerciseId}-{request.RequestId:N}.zip",
            output.ToArray()));
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        entry.LastWriteTime = StableTimestamp;
        using Stream stream = entry.Open();
        using var writer = new StreamWriter(stream, Utf8WithoutBom, bufferSize: 4_096, leaveOpen: false);
        writer.Write(content);
    }

    private sealed record ManualManifest(
        int SchemaVersion,
        Guid RequestId,
        string ExerciseId,
        int ExerciseVersion,
        string ContentRevision,
        IReadOnlyList<string> SourceFiles);
}
