using System.Text.Json;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.Content;
using ForgeDotNet.CodeRunner;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.IntegrationTests;

[Trait("Category", "ContentS1S10")]
public sealed class ContentS1S10CoverageTests
{
    [Fact]
    public async Task EveryExerciseRunnerContractCanBeMaterializedWithoutPrivateLeak()
    {
        string contentRoot = FindContentRoot();
        string catalogRoot = Path.Combine(contentRoot, "reference");
        var options = new ContentValidationOptions { ContentRootPath = contentRoot };
        using var provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
            new FileSystemContentValidationService(options), options));
        ContentCatalogReloadResult reload = await provider.ReloadAsync(catalogRoot);
        Assert.True(reload.Succeeded, string.Join(Environment.NewLine, reload.Issues.Select(issue => issue.Message)));
        var exerciseSource = new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
        {
            ContentRootPath = contentRoot,
            CatalogDirectoryPath = catalogRoot,
        });
        var runnerSource = new FileSystemDockerRunSpecificationSource(exerciseSource, new DockerRunContentOptions
        {
            ContentRootPath = contentRoot,
            CatalogDirectoryPath = catalogRoot,
        });

        foreach (string manifest in Directory.GetFiles(
            Path.Combine(catalogRoot, "exercises"), "exercise.json", SearchOption.AllDirectories))
        {
            string id = Path.GetFileName(Path.GetDirectoryName(manifest)!);
            var exercise = await exerciseSource.GetAsync(id);
            Assert.NotNull(exercise);
            var request = new CodeRunRequest(
                Guid.NewGuid(), exercise.Id, exercise.Version, exercise.Revision,
                [new CodeRunSourceFile("Submission.cs", exercise.Starter)]);
            DockerRunSpecification? specification = await runnerSource.GetAsync(request);
            Assert.NotNull(specification);
            using JsonDocument suite = JsonDocument.Parse(specification.SuiteDefinition!);
            Assert.InRange(suite.RootElement.GetProperty("cases").GetArrayLength(), 4, 20);
            Assert.DoesNotContain("Hidden_", JsonSerializer.Serialize(new
            {
                exercise.Id,
                exercise.Title,
                exercise.Statement,
                exercise.Starter,
            }), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CoverageVolumesAndWeeksMatchTheFrozenMatrix()
    {
        string root = FindContentRoot();
        string catalog = Path.Combine(root, "reference");

        string[] lessons = Directory.GetFiles(
            Path.Combine(catalog, "curriculum", "lessons"), "lesson.json", SearchOption.AllDirectories);
        string[] exercises = Directory.GetFiles(
            Path.Combine(catalog, "exercises"), "exercise.json", SearchOption.AllDirectories);
        string[] debug = Directory.GetFiles(
            Path.Combine(catalog, "debugging"), "scenario.json", SearchOption.AllDirectories);
        string[] sql = Directory.GetFiles(Path.Combine(root, "sql"), "scenario.json", SearchOption.AllDirectories);
        string[] projects = Directory.GetFiles(Path.Combine(catalog, "projects"), "*.json", SearchOption.TopDirectoryOnly);
        string[] interviews = Directory.GetFiles(Path.Combine(catalog, "interviews"), "*.json", SearchOption.TopDirectoryOnly);
        string[] exams = Directory.GetFiles(Path.Combine(root, "exams"), "exam.json", SearchOption.AllDirectories);

        Assert.Equal(30, lessons.Length);
        Assert.Equal(85, exercises.Length);
        Assert.Equal(25, debug.Length);
        Assert.Equal(40, sql.Length);
        Assert.Equal(5, projects.Length);
        Assert.Equal(84, interviews.Length);
        Assert.Equal(4, exams.Length);

        int[] lessonWeeks = lessons.Select(path => Read(path).RootElement.GetProperty("week").GetInt32()).ToArray();
        Assert.All(lessonWeeks, week => Assert.InRange(week, 1, 10));
        for (int week = 1; week <= 10; week++) Assert.Equal(3, lessonWeeks.Count(value => value == week));

        int[] sqlWeeks = sql.Select(path =>
            Read(Path.Combine(Path.GetDirectoryName(path)!, "tests", "contract.json"))
                .RootElement.GetProperty("week").GetInt32()).ToArray();
        Assert.Equal(14, sqlWeeks.Count(value => value == 8));
        Assert.Equal(13, sqlWeeks.Count(value => value == 9));
        Assert.Equal(13, sqlWeeks.Count(value => value == 10));

        int[] projectWeeks = projects.SelectMany(path => Read(path).RootElement.GetProperty("weeks")
            .EnumerateArray().Select(value => value.GetInt32())).ToArray();
        Assert.Equal([2, 4, 5, 7, 10], projectWeeks.Order().ToArray());
    }

    [Fact]
    public void EveryLessonIsAutonomousAndEveryExerciseHasPrivateUsefulProofs()
    {
        string catalog = Path.Combine(FindContentRoot(), "reference");
        string[] requiredHeadings =
        [
            "## Objectif observable", "## Prérequis", "## Intuition", "## Explication",
            "## Exemple commenté", "## Contre-exemple et erreur fréquente",
            "## Vérification de compréhension", "## Exercice guidé", "## Exercice autonome",
            "## Débogage", "## Entretien", "## Résumé", "## Cartes de révision", "## Test de maîtrise",
        ];
        foreach (string manifestPath in Directory.GetFiles(
            Path.Combine(catalog, "curriculum", "lessons"), "lesson.json", SearchOption.AllDirectories))
        {
            string markdown = File.ReadAllText(Path.Combine(Path.GetDirectoryName(manifestPath)!, "lesson.md"));
            int previous = -1;
            foreach (string heading in requiredHeadings)
            {
                int current = markdown.IndexOf(heading, StringComparison.Ordinal);
                Assert.True(current > previous, $"Section absente ou désordonnée dans {manifestPath}: {heading}");
                previous = current;
            }
            Assert.Contains(":::quiz", markdown, StringComparison.Ordinal);
            Assert.Contains("correct=", markdown, StringComparison.Ordinal);
        }

        var interviewIds = Directory.GetFiles(Path.Combine(catalog, "interviews"), "*.json")
            .Select(path => Read(path).RootElement.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        foreach (string manifestPath in Directory.GetFiles(
            Path.Combine(catalog, "exercises"), "exercise.json", SearchOption.AllDirectories))
        {
            string directory = Path.GetDirectoryName(manifestPath)!;
            JsonElement manifest = Read(manifestPath).RootElement;
            string id = manifest.GetProperty("id").GetString()!;
            Assert.Equal(id, Path.GetFileName(directory));
            Assert.Contains(manifest.GetProperty("interviewQuestionId").GetString()!, interviewIds);
            foreach (string relative in new[]
            {
                "statement.md", "explanation.md", "review-cards.md", "starter/Submission.cs",
                "solution/Submission.cs", "tests/runner.json", "tests/visible/cases.json", "tests/hidden/cases.json",
            })
            {
                FileInfo file = new(Path.Combine(directory, relative));
                Assert.True(file.Exists && file.Length > 0, $"Artefact absent ou vide : {id}/{relative}");
            }

            string starter = File.ReadAllText(Path.Combine(directory, "starter", "Submission.cs"));
            string solution = File.ReadAllText(Path.Combine(directory, "solution", "Submission.cs"));
            Assert.NotEqual(starter, solution);
            Assert.DoesNotContain("NotImplementedException", solution, StringComparison.Ordinal);
            using JsonDocument visible = Read(Path.Combine(directory, "tests", "visible", "cases.json"));
            using JsonDocument hidden = Read(Path.Combine(directory, "tests", "hidden", "cases.json"));
            Assert.InRange(visible.RootElement.GetProperty("cases").GetArrayLength(), 2, 20);
            Assert.InRange(hidden.RootElement.GetProperty("cases").GetArrayLength(), 2, 20);
            Assert.NotEqual(visible.RootElement.GetRawText(), hidden.RootElement.GetRawText());
        }
    }

    [Fact]
    public void LearnerContentContainsNoPlaceholderOrS11Topic()
    {
        string root = FindContentRoot();
        string[] inspected = Directory.GetFiles(Path.Combine(root, "reference"), "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "sql"), "*", SearchOption.AllDirectories))
            .Where(path => Path.GetExtension(path) is ".md" or ".json" or ".sql")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}starter{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        string text = string.Join('\n', inspected.Select(File.ReadAllText));
        foreach (string placeholder in new[] { "lorem ipsum", "TODO", "à venir", "solution sensible ne doit jamais" })
            Assert.DoesNotContain(placeholder, text, StringComparison.OrdinalIgnoreCase);
        foreach (string futureTopic in new[] { "ASP.NET", "OpenAPI", "authentification", "GitHub Actions", "Azure App Service" })
            Assert.DoesNotContain(futureTopic, text, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllBytes(path));

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
