using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Fige la piste senior S25-S32, portee par son propre manifeste de parcours
/// <c>forge-senior-reference.json</c>, distinct du socle junior. Ces tests sont propres a la piste :
/// ils ne partagent pas la matrice figee du parcours junior, dont la valeur est de rester le releve
/// des vingt-quatre semaines.
/// </summary>
[Trait("Category", "ContentSenior")]
public sealed class ContentSeniorCoverageTests
{
    private static readonly string ContentRoot = FindContentRoot();
    private static readonly string CatalogRoot = Path.Combine(ContentRoot, "reference");

    private static readonly string[] RequiredLessonHeadings =
    [
        "## Objectif observable", "## Prérequis", "## Intuition", "## Explication",
        "## Exemple commenté", "## Contre-exemple et erreur fréquente",
        "## Vérification de compréhension", "## Exercice guidé", "## Exercice autonome",
        "## Débogage", "## Entretien", "## Résumé", "## Cartes de révision", "## Test de maîtrise",
    ];

    [Fact]
    public void SeniorTrackCoversEightWeeksWithOneLessonAndOneExerciseEach()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-senior-reference.json"));
        JsonElement root = curriculum.RootElement;
        JsonElement[] modules = root.GetProperty("modules").EnumerateArray().ToArray();

        Assert.Equal("forge-senior-reference", root.GetProperty("id").GetString());
        Assert.Equal(32, root.GetProperty("weeks").GetInt32());
        Assert.Equal(8, modules.Length);
        Assert.Equal(Enumerable.Range(25, 8), modules.Select(module => module.GetProperty("weeks")[0].GetInt32()));
        Assert.All(modules, module => Assert.Equal(1, module.GetProperty("lessonIds").GetArrayLength()));
        Assert.All(modules, module => Assert.Equal(1, module.GetProperty("exerciseIds").GetArrayLength()));

        // La premiere semaine senior n'a pas de prerequis dans ce parcours ; les suivantes chainent.
        Assert.Empty(modules[0].GetProperty("prerequisites").EnumerateArray());
        for (int index = 1; index < modules.Length; index++)
        {
            Assert.Equal(
                modules[index - 1].GetProperty("id").GetString(),
                Assert.Single(modules[index].GetProperty("prerequisites").EnumerateArray()).GetString());
        }

        string[] lessonIds = modules.SelectMany(module => module.GetProperty("lessonIds").EnumerateArray())
            .Select(value => value.GetString()!).ToArray();
        string[] exerciseIds = modules.SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!).ToArray();
        Assert.All(lessonIds, id => Assert.StartsWith("senior-", id));
        Assert.All(exerciseIds, id => Assert.StartsWith("senior-", id));

        foreach (string lessonId in lessonIds)
        {
            string markdown = File.ReadAllText(
                Path.Combine(CatalogRoot, "curriculum", "lessons", lessonId, "lesson.md"));
            int previous = -1;
            foreach (string heading in RequiredLessonHeadings)
            {
                int current = markdown.IndexOf(heading, StringComparison.Ordinal);
                Assert.True(current > previous, $"Section absente ou desordonnee dans {lessonId} : {heading}");
                previous = current;
            }

            Assert.Contains(":::quiz", markdown, StringComparison.Ordinal);
        }

        foreach (string exerciseId in exerciseIds)
        {
            string directory = Path.Combine(CatalogRoot, "exercises", exerciseId);
            using JsonDocument manifest = Read(Path.Combine(directory, "exercise.json"));
            JsonElement exercise = manifest.RootElement;
            Assert.Equal([1, 2, 3, 4], exercise.GetProperty("hints").EnumerateArray()
                .Select(hint => hint.GetProperty("level").GetInt32()).ToArray());
            Assert.Equal(6, exercise.GetProperty("reflectionFields").GetArrayLength());
            Assert.Contains(exercise.GetProperty("variantId").GetString()!, exerciseIds);
            Assert.True(Read(Path.Combine(directory, "tests", "visible", "cases.json"))
                .RootElement.GetProperty("cases").GetArrayLength() >= 3);
            Assert.True(Read(Path.Combine(directory, "tests", "hidden", "cases.json"))
                .RootElement.GetProperty("cases").GetArrayLength() >= 4);
            Assert.Contains("NotImplementedException",
                File.ReadAllText(Path.Combine(directory, "starter", "Submission.cs")), StringComparison.Ordinal);
            Assert.DoesNotContain("NotImplementedException",
                File.ReadAllText(Path.Combine(directory, "solution", "Submission.cs")), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SeniorTrackWiresTheCodeReviewProducerAndTheLegacyDebugScenario()
    {
        // S31 : le projet de revue produit l'accomplissement code-review, sans producteur jusqu'ici.
        using JsonDocument project = Read(
            Path.Combine(CatalogRoot, "projects", "project-code-review-001", "project.json"));
        Assert.Equal("code-review", project.RootElement.GetProperty("achievementKey").GetString());
        Assert.True(project.RootElement.GetProperty("acceptanceSuites").GetArrayLength() >= 1);
        Assert.True(File.Exists(Path.Combine(CatalogRoot, "projects", "project-code-review-001", "starter", "Submission.cs")));
        Assert.True(File.Exists(Path.Combine(CatalogRoot, "projects", "project-code-review-001", "solution", "Submission.cs")));

        // S32 : le laboratoire de debogage sur base existante.
        using JsonDocument scenario = Read(
            Path.Combine(CatalogRoot, "debugging", "senior-legacy-debug-001", "scenario.json"));
        Assert.Equal("senior-legacy-debug-001", scenario.RootElement.GetProperty("id").GetString());
        Assert.True(File.Exists(Path.Combine(CatalogRoot, "debugging", "senior-legacy-debug-001", "broken", "Submission.cs")));
        Assert.True(File.Exists(Path.Combine(CatalogRoot, "debugging", "senior-legacy-debug-001", "correction", "Submission.cs")));

        // Les huit exercices senior sont tirables par l'examen senior.
        using JsonDocument exam = Read(Path.Combine(ContentRoot, "exams", "senior-readiness-v1", "exam.json"));
        JsonElement eligible = exam.RootElement.GetProperty("eligibleExerciseIds");
        Assert.Equal(8, eligible.GetArrayLength());
        Assert.All(eligible.EnumerateArray(), id => Assert.StartsWith("senior-", id.GetString()!));
    }

    private static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllBytes(path));

    private static string FindContentRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "schemas", "lesson.schema.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Racine content introuvable.");
    }
}
