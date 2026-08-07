using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.IntegrationTests;

[Collection(DockerCodeRunnerSecurityTestGroup.CollectionName)]
[Trait("Category", "DebugLabRunner")]
public sealed class DebugLabDockerRunnerTests(DockerSecurityFixture dockerFixture)
{
    [Fact]
    public async Task EveryScenarioIsBrokenThenRepairedByItsRegressionSuite()
    {
        using DebugContentFixture content = await DebugContentFixture.CreateAsync();
        var specificationSource = new FileSystemDockerRunSpecificationSource(
            content.PracticeSource,
            new DockerRunContentOptions
            {
                ContentRootPath = content.ContentRoot,
                CatalogDirectoryPath = content.CatalogRoot,
            },
            content.Source);
        string workspace = Path.Combine(
            Path.GetTempPath(), "ForgeDotNet.DebugLabRunner", Guid.NewGuid().ToString("N"));
        using var runner = new DockerCodeRunner(
            new DockerCodeRunnerOptions
            {
                DockerContext = dockerFixture.DockerContext,
                ImageReference = dockerFixture.ImageReference,
                WorkspaceRootPath = workspace,
                MaximumConcurrency = 1,
                TestTimeout = TimeSpan.FromSeconds(30),
            },
            specificationSource,
            TimeProvider.System);

        IReadOnlyList<DebugScenario> scenarios = await content.Source.ListAsync();
        foreach (DebugScenario scenario in scenarios.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            CodeRunResult broken = await runner.RunAsync(DebugLabContentTests.Request(scenario, scenario.BrokenSource));
            Assert.True(
                broken.Status is CodeRunStatus.TestsFailed or CodeRunStatus.CompilationFailed,
                $"{scenario.Id} doit être réellement cassé, statut={broken.Status}, compilation={broken.Compilation.Output.Text}, tests={broken.Tests.Output.Text}");

            CodeRunResult repaired = await runner.RunAsync(DebugLabContentTests.Request(scenario, scenario.CorrectionSource));
            Assert.True(
                repaired.Status == CodeRunStatus.Succeeded,
                $"{scenario.Id} corrigé doit réussir, statut={repaired.Status}, compilation={repaired.Compilation.Output.Text}, tests={repaired.Tests.Output.Text}");
            Assert.InRange(repaired.Tests.TotalCount, 4, 6);
            Assert.Equal(repaired.Tests.TotalCount, repaired.Tests.PassedCount);
            Assert.Equal(0, repaired.Tests.HiddenFailureCount);
            Assert.DoesNotContain("Hidden_", repaired.Tests.Output.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("/workspace", repaired.Tests.Output.Text, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Empty(Directory.Exists(workspace) ? Directory.EnumerateFileSystemEntries(workspace) : []);
    }
}
