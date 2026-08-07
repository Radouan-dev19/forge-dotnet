using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.IntegrationTests;

[Collection(DockerCodeRunnerSecurityTestGroup.CollectionName)]
[Trait("Category", "InitialCSharpContent")]
public sealed class InitialCSharpContentTests(DockerSecurityFixture dockerFixture)
{
    [Fact]
    public async Task AllOneHundredThirtyFivePublishedSolutionsPassAndStartersCompileWithoutPassing()
    {
        using ContentEnvironment content = await ContentEnvironment.CreateAsync();
        var specificationSource = new FileSystemDockerRunSpecificationSource(
            content.ExerciseSource,
            new DockerRunContentOptions
            {
                ContentRootPath = content.ContentRoot,
                CatalogDirectoryPath = content.CatalogRoot,
            });
        string workspace = Path.Combine(
            Path.GetTempPath(),
            "ForgeDotNet.S1S10CSharpContent",
            Guid.NewGuid().ToString("N"));
        using var runner = new DockerCodeRunner(
            new DockerCodeRunnerOptions
            {
                DockerContext = dockerFixture.DockerContext,
                ImageReference = dockerFixture.ImageReference,
                WorkspaceRootPath = workspace,
                MaximumConcurrency = 2,
                TestTimeout = TimeSpan.FromSeconds(30),
            },
            specificationSource,
            TimeProvider.System);

        string[] exerciseIds = Directory.GetDirectories(Path.Combine(content.CatalogRoot, "exercises"))
            .Where(path => File.Exists(Path.Combine(path, "exercise.json")))
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray()!;
        Assert.Equal(135, exerciseIds.Length);

        await Task.WhenAll(exerciseIds.Select(async exerciseId =>
        {
            var exercise = await content.ExerciseSource.GetAsync(exerciseId);
            Assert.NotNull(exercise);
            string solution = await File.ReadAllTextAsync(Path.Combine(
                content.CatalogRoot, "exercises", exerciseId, "solution", "Submission.cs"));

            CodeRunResult solutionResult;
            try
            {
                solutionResult = await runner.RunAsync(Request(exercise, solution));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Contrat runner invalide pour {exerciseId}.", exception);
            }
            Assert.True(
                solutionResult.Status == CodeRunStatus.Succeeded,
                $"{exerciseId}: solution={solutionResult.Status}; compilation={solutionResult.Compilation.Output.Text}; tests={solutionResult.Tests.Output.Text}");
            Assert.Equal(solutionResult.Tests.TotalCount, solutionResult.Tests.PassedCount);
            Assert.True(solutionResult.Tests.TotalCount >= 4);

            CodeRunResult starterResult = await runner.RunAsync(Request(exercise, exercise.Starter));
            Assert.True(
                starterResult.Compilation.Status == CodeRunStageStatus.Succeeded,
                $"{exerciseId}: compilation starter={starterResult.Compilation.Status}; sortie={starterResult.Compilation.Output.Text}");
            Assert.True(
                starterResult.Status == CodeRunStatus.TestsFailed,
                $"{exerciseId}: starter={starterResult.Status}; compilation={starterResult.Compilation.Output.Text}; tests={starterResult.Tests.Output.Text}");
        }));

        Assert.Empty(Directory.Exists(workspace)
            ? Directory.EnumerateFileSystemEntries(workspace)
            : []);
    }

    [Theory]
    [MemberData(nameof(Exercises))]
    public async Task SolutionStarterAndHardcodedSubmissionHaveUsefulProofs(
        string exerciseId,
        string hardcodedSource)
    {
        using ContentEnvironment content = await ContentEnvironment.CreateAsync();
        var specificationSource = new FileSystemDockerRunSpecificationSource(
            content.ExerciseSource,
            new DockerRunContentOptions
            {
                ContentRootPath = content.ContentRoot,
                CatalogDirectoryPath = content.CatalogRoot,
            });
        string workspace = Path.Combine(
            Path.GetTempPath(),
            "ForgeDotNet.InitialCSharpContent",
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

        var exercise = await content.ExerciseSource.GetAsync(exerciseId);
        Assert.NotNull(exercise);
        string solution = await File.ReadAllTextAsync(Path.Combine(
            content.CatalogRoot, "exercises", exerciseId, "solution", "Submission.cs"));

        CodeRunResult solutionResult = await runner.RunAsync(Request(exercise, solution));
        Assert.True(
            solutionResult.Status == CodeRunStatus.Succeeded,
            $"{exerciseId}: {solutionResult.Summary} Compilation: {solutionResult.Compilation.Output.Text} Tests: {solutionResult.Tests.Output.Text}");
        Assert.Equal(7, solutionResult.Tests.TotalCount);
        Assert.Equal(7, solutionResult.Tests.PassedCount);

        CodeRunResult starterResult = await runner.RunAsync(Request(exercise, exercise.Starter));
        Assert.Equal(CodeRunStatus.TestsFailed, starterResult.Status);
        Assert.Equal(CodeRunStageStatus.Succeeded, starterResult.Compilation.Status);

        CodeRunResult hardcodedResult = await runner.RunAsync(Request(exercise, hardcodedSource));
        Assert.Equal(CodeRunStatus.TestsFailed, hardcodedResult.Status);
        Assert.True(hardcodedResult.Tests.HiddenFailureCount > 0);
        string publicResult = string.Join(' ',
            hardcodedResult.Summary,
            hardcodedResult.Tests.Output.Text,
            string.Join(' ', hardcodedResult.Tests.VisibleFailures.Select(failure => $"{failure.Name} {failure.Message}")));
        Assert.DoesNotContain("Hidden_", publicResult, StringComparison.Ordinal);

        Assert.Empty(Directory.Exists(workspace)
            ? Directory.EnumerateFileSystemEntries(workspace)
            : []);
    }

    public static TheoryData<string, string> Exercises => new()
    {
        {
            "csharp-price-conversion-001",
            "public static class Submission { public static int ToCents(decimal amount) => 1234; }"
        },
        {
            "csharp-shipping-decision-001",
            "public static class Submission { public static decimal ShippingCost(decimal orderTotal, bool isExpress) => 4.90m; }"
        },
        {
            "csharp-loop-range-sum-001",
            "public static class Submission { public static int SumInclusive(int start, int end) => 18; }"
        },
        {
            "csharp-method-multiples-001",
            "public static class Submission { public static int CountMultiples(int start, int end, int divisor) => 3; }"
        },
        {
            "csharp-array-differences-001",
            "public static class Submission { public static int[] Differences(int[] values) => new[] { 5, -2, 4 }; }"
        },
        {
            "csharp-list-distinct-001",
            "public static class Submission { public static System.Collections.Generic.List<int> DistinctInOrder(System.Collections.Generic.List<int> values) => new() { 3, 1, 2 }; }"
        },
        {
            "csharp-dictionary-stock-001",
            "public static class Submission { public static System.Collections.Generic.Dictionary<string, int> MergeStock(System.Collections.Generic.Dictionary<string, int> stock, System.Collections.Generic.Dictionary<string, int> incoming) => new() { [\"pen\"] = 5, [\"book\"] = 1 }; }"
        },
        {
            "csharp-string-frequency-001",
            "public static class Submission { public static System.Collections.Generic.Dictionary<string, int> CountWords(string text) => new() { [\"chat\"] = 2, [\"chien\"] = 1 }; }"
        },
        {
            "csharp-date-business-days-001",
            "public static class Submission { public static int CountBusinessDays(System.DateOnly start, System.DateOnly end) => 5; }"
        },
        {
            "csharp-date-expiry-001",
            "public static class Submission { public static bool IsExpired(System.DateOnly dueDate, System.DateOnly today, int graceDays) => false; }"
        },
    };

    private static CodeRunRequest Request(ForgeDotNet.Domain.Practice.PracticeExercise exercise, string source) => new(
        Guid.NewGuid(),
        exercise.Id,
        exercise.Version,
        exercise.Revision,
        Array.AsReadOnly([new CodeRunSourceFile("Submission.cs", source)]));

    private sealed class ContentEnvironment(
        ContentCatalogProvider provider,
        FileSystemPracticeExerciseSource source,
        string contentRoot,
        string catalogRoot) : IDisposable
    {
        public FileSystemPracticeExerciseSource ExerciseSource { get; } = source;

        public string ContentRoot { get; } = contentRoot;

        public string CatalogRoot { get; } = catalogRoot;

        public static async Task<ContentEnvironment> CreateAsync()
        {
            string contentRoot = FindContentRoot();
            string catalogRoot = Path.Combine(contentRoot, "reference");
            var options = new ContentValidationOptions { ContentRootPath = contentRoot };
            var validation = new FileSystemContentValidationService(options);
            var provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(validation, options));
            ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogRoot);
            Assert.True(reload.Succeeded);
            var source = new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
            {
                ContentRootPath = contentRoot,
                CatalogDirectoryPath = catalogRoot,
            });
            return new ContentEnvironment(provider, source, contentRoot, catalogRoot);
        }

        public void Dispose() => provider.Dispose();
    }

    private static string FindContentRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "schemas", "exercise.schema.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("La racine de contenu est introuvable.");
    }
}
