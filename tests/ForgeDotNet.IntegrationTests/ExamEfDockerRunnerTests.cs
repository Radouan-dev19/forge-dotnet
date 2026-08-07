using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Exams;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.IntegrationTests;

[Collection(EfDockerCodeRunnerTestGroup.CollectionName)]
[Trait("Category", "ContentS1S10Docker")]
public sealed class ExamEfDockerRunnerTests(DockerSecurityFixture dockerFixture)
{
    [Fact]
    public async Task EfExamStartersFailAndSolutionsPassInsideIsolatedRunner()
    {
        string contentRoot = FindContentRoot();
        string catalogRoot = Path.Combine(contentRoot, "reference");
        var validationOptions = new ContentValidationOptions { ContentRootPath = contentRoot };
        using var provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
            new FileSystemContentValidationService(validationOptions),
            validationOptions));
        ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogRoot);
        Assert.True(reload.Succeeded, string.Join(Environment.NewLine, reload.Issues.Select(item => item.Message)));
        var practiceSource = new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
        {
            ContentRootPath = contentRoot,
            CatalogDirectoryPath = catalogRoot,
        });
        var bank = new FileSystemExamBankSource(practiceSource, new ExamBankOptions
        {
            ContentRootPath = contentRoot,
            BankDirectoryPath = Path.Combine(contentRoot, "exams"),
        });
        ExamBlueprint exam = await bank.GetAsync("sql-ef-core-v1")
            ?? throw new InvalidDataException("Examen 4 absent.");
        ExamCandidate[] candidates = exam.Candidates
            .Where(item => item.SubmissionKind == ExamSubmissionKind.CSharp)
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, candidates.Length);

        var specificationSource = new FileSystemDockerRunSpecificationSource(
            practiceSource,
            new DockerRunContentOptions
            {
                ContentRootPath = contentRoot,
                CatalogDirectoryPath = catalogRoot,
                SqlDirectoryPath = Path.Combine(contentRoot, "sql"),
            });
        string workspace = Path.Combine(
            Path.GetTempPath(),
            "ForgeDotNet.ExamEfRunner",
            Guid.NewGuid().ToString("N"));
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

        foreach (ExamCandidate candidate in candidates)
        {
            CodeRunResult starter = await runner.RunAsync(Request(candidate, candidate.StarterCode));
            Assert.True(
                starter.Status == CodeRunStatus.TestsFailed,
                $"Starter {candidate.ItemId}: {starter.Status}; compilation={starter.Compilation.Output.Text}; tests={starter.Tests.Output.Text}");
            Assert.True(starter.Tests.HiddenFailuresRedacted);
            Assert.True(starter.Tests.HiddenFailureCount > 0);

            string solution = await File.ReadAllTextAsync(Path.Combine(
                contentRoot,
                "sql",
                candidate.ItemId,
                "exam",
                "solution",
                "Submission.cs"));
            CodeRunResult repaired = await runner.RunAsync(Request(candidate, solution));
            Assert.True(
                repaired.Status == CodeRunStatus.Succeeded,
                $"{candidate.ItemId}: {repaired.Status}; compilation={repaired.Compilation.Output.Text}; tests={repaired.Tests.Output.Text}");
            Assert.Equal(repaired.Tests.TotalCount, repaired.Tests.PassedCount);
            Assert.Equal(0, repaired.Tests.HiddenFailureCount);
            Assert.DoesNotContain("/workspace", repaired.Tests.Output.Text, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Empty(Directory.Exists(workspace) ? Directory.EnumerateFileSystemEntries(workspace) : []);
    }

    private static CodeRunRequest Request(ExamCandidate candidate, string source) => new(
        Guid.NewGuid(),
        candidate.ItemId,
        candidate.ItemVersion,
        candidate.ContentRevision,
        [new CodeRunSourceFile("Submission.cs", source)]);

    private static string FindContentRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "schemas", "lesson.schema.json"))) return candidate;
        }

        throw new DirectoryNotFoundException("Racine content introuvable.");
    }
}

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class EfDockerCodeRunnerTestGroup : ICollectionFixture<DockerSecurityFixture>
{
    public const string CollectionName = "EfCodeRunnerSecurityDocker";
}
