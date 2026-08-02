using System.IO.Compression;
using System.Text;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.CodeRunner;

namespace ForgeDotNet.UnitTests;

public sealed class DockerCodeRunnerConfigurationTests
{
    private const string ImageId = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void OptionsRequireImmutableImageAndBoundedQuotas()
    {
        DockerCodeRunnerOptions valid = CreateOptions();
        valid.Validate();

        Assert.Throws<InvalidDataException>(() => (valid with { ImageReference = "forge-dotnet-runner:test" }).Validate());
        Assert.Throws<InvalidDataException>(() => (valid with { MemoryBytes = 1024 * DockerCodeRunnerOptions.Mebibyte }).Validate());
        Assert.Throws<InvalidDataException>(() => (valid with { PidsLimit = 65 }).Validate());
        Assert.Throws<InvalidDataException>(() => (valid with { MaximumConcurrency = 0 }).Validate());
        string volumeRoot = Path.GetPathRoot(valid.WorkspaceRootPath)!;
        Assert.Throws<InvalidDataException>(() => (valid with { WorkspaceRootPath = volumeRoot }).Validate());
    }

    [Fact]
    public void ModeParserIsFailClosed()
    {
        Assert.Equal(CodeRunnerMode.Manual, CodeRunnerModeParser.Parse(null));
        Assert.Equal(CodeRunnerMode.Docker, CodeRunnerModeParser.Parse("Docker"));
        Assert.Equal(CodeRunnerMode.Deterministic, CodeRunnerModeParser.Parse("deterministic"));
        Assert.Throws<InvalidDataException>(() => CodeRunnerModeParser.Parse("LocalShell"));
    }

    [Fact]
    public async Task ManualPackageContainsOnlyPublicMetadataAndSubmittedSources()
    {
        var exporter = new ManualCodeRunPackageExporter();
        CodeRunRequest request = CreateRequest();

        ManualCodeRunPackage package = await exporter.ExportAsync(request);

        Assert.EndsWith(".zip", package.FileName, StringComparison.Ordinal);
        using var stream = new MemoryStream(package.Content.ToArray());
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        string[] entries = archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(["README.md", "forge-manual.json", "sources/Submission.cs"], entries);
        string readme = await ReadEntryAsync(archive, "README.md");
        string manifest = await ReadEntryAsync(archive, "forge-manual.json");
        string source = await ReadEntryAsync(archive, "sources/Submission.cs");
        Assert.Contains("aucune preuve automatique", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(request.ExerciseId, manifest, StringComparison.Ordinal);
        Assert.Equal(request.SourceFiles[0].Content, source);
        Assert.DoesNotContain("hidden", manifest, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution", manifest, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ManualModeReturnsUnavailableWithoutPretendingValidation()
    {
        var runner = new UnavailableCodeRunner(TimeProvider.System);

        CodeRunResult result = await runner.RunAsync(CreateRequest());

        Assert.Equal(CodeRunStatus.Unavailable, result.Status);
        Assert.Equal(CodeRunStageStatus.Unavailable, result.Compilation.Status);
        Assert.Equal(CodeRunStageStatus.NotRun, result.Tests.Status);
        Assert.Contains("manuel", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validé", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    private static DockerCodeRunnerOptions CreateOptions() => new()
    {
        ImageReference = ImageId,
        WorkspaceRootPath = Path.Combine(Path.GetTempPath(), "ForgeDotNet.UnitRunner", "runner-workspaces"),
    };

    private static CodeRunRequest CreateRequest() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "runner-security-fixture",
        1,
        new string('A', 64),
        Array.AsReadOnly([
            new CodeRunSourceFile(
                "Submission.cs",
                "public static class Submission { public static int Visible() => 42; public static int Hidden() => 7; }"),
        ]));

    private static async Task<string> ReadEntryAsync(ZipArchive archive, string name)
    {
        ZipArchiveEntry entry = Assert.Single(archive.Entries, item => item.FullName == name);
        await using Stream stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
