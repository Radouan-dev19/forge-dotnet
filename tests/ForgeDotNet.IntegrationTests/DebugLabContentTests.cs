using System.Text.Json;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.DebugLab;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.IntegrationTests;

public sealed class DebugLabContentTests
{
    [Fact]
    public async Task TwentyNineScenariosAreValidCompleteAndMappedToGenericRunnerSuites()
    {
        using DebugContentFixture fixture = await DebugContentFixture.CreateAsync();

        IReadOnlyList<DebugScenario> scenarios = await fixture.Source.ListAsync();

        Assert.Equal(29, scenarios.Count);
        Assert.Equal(29, scenarios.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(scenarios, scenario =>
        {
            DebugLabRules.ValidateScenario(scenario);
            Assert.Equal(64, scenario.Revision.Length);
            Assert.DoesNotContain("C:\\", scenario.SanitizedLogs, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/home/", scenario.SanitizedLogs, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/workspace/", scenario.SanitizedLogs, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Bearer ", scenario.SanitizedLogs, StringComparison.OrdinalIgnoreCase);
        });

        var runnerSource = new FileSystemDockerRunSpecificationSource(
            fixture.PracticeSource,
            new DockerRunContentOptions
            {
                ContentRootPath = fixture.ContentRoot,
                CatalogDirectoryPath = fixture.CatalogRoot,
            },
            fixture.Source);
        foreach (DebugScenario scenario in scenarios)
        {
            DockerRunSpecification? specification = await runnerSource.GetAsync(Request(scenario, scenario.BrokenSource));
            Assert.NotNull(specification);
            Assert.Equal(scenario.Id, specification.SuiteId);
            using JsonDocument suite = JsonDocument.Parse(specification.SuiteDefinition!);
            int caseCount = suite.RootElement.GetProperty("cases").GetArrayLength();
            Assert.InRange(caseCount, 4, 6);
            Assert.InRange(suite.RootElement.GetProperty("cases").EnumerateArray().Count(item => item.GetProperty("isVisible").GetBoolean()), 2, 3);
            Assert.DoesNotContain(scenario.CorrectionSource, specification.SuiteDefinition, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task InitialActivityProjectionNeverContainsProtectedCorrectionOrHiddenCases()
    {
        using DebugContentFixture fixture = await DebugContentFixture.CreateAsync();
        DebugScenario scenario = (await fixture.Source.ListAsync()).Single(item => item.Id == "debug-null-reference-001");
        DebugLabActivity activity = DebugLabRules.Start(Guid.NewGuid(), scenario, DateTimeOffset.UtcNow);

        string publicProjection = JsonSerializer.Serialize(new
        {
            scenario.Id,
            scenario.Title,
            scenario.Ticket,
            scenario.ExpectedBehavior,
            scenario.SanitizedLogs,
            scenario.Checklist,
            scenario.ObservationQuestions,
            scenario.BrokenSource,
            activity.State,
        });

        Assert.DoesNotContain(scenario.CorrectionSource, publicProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("Hidden_", publicProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/hidden", publicProjection, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correction/", publicProjection, StringComparison.OrdinalIgnoreCase);
    }

    internal static CodeRunRequest Request(DebugScenario scenario, string source) => new(
        Guid.NewGuid(), scenario.Id, scenario.Version, scenario.Revision,
        Array.AsReadOnly([new CodeRunSourceFile("Submission.cs", source)]));
}

internal sealed class DebugContentFixture(
    ContentCatalogProvider provider,
    FileSystemDebugScenarioSource source,
    FileSystemPracticeExerciseSource practiceSource,
    string contentRoot,
    string catalogRoot) : IDisposable
{
    public FileSystemDebugScenarioSource Source { get; } = source;
    public FileSystemPracticeExerciseSource PracticeSource { get; } = practiceSource;
    public string ContentRoot { get; } = contentRoot;
    public string CatalogRoot { get; } = catalogRoot;

    public static async Task<DebugContentFixture> CreateAsync()
    {
        string contentRoot = FindContentRoot();
        string catalogRoot = Path.Combine(contentRoot, "reference");
        var validationOptions = new ContentValidationOptions { ContentRootPath = contentRoot };
        var provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
            new FileSystemContentValidationService(validationOptions), validationOptions));
        ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogRoot);
        Assert.True(reload.Succeeded, string.Join(Environment.NewLine, reload.Issues.Select(issue => issue.Message)));
        var source = new FileSystemDebugScenarioSource(provider, new DebugContentOptions
        {
            ContentRootPath = contentRoot,
            CatalogDirectoryPath = catalogRoot,
        });
        var practiceSource = new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
        {
            ContentRootPath = contentRoot,
            CatalogDirectoryPath = catalogRoot,
        });
        return new DebugContentFixture(provider, source, practiceSource, contentRoot, catalogRoot);
    }

    public void Dispose() => provider.Dispose();

    private static string FindContentRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "schemas", "debug.schema.json"))) return candidate;
        }
        throw new DirectoryNotFoundException("La racine de contenu DebugLab est introuvable.");
    }
}
