using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

[Trait("Category", "ContentS11S20")]
public sealed class ContentS11S20CoverageTests
{
    private static readonly string[] S11S20LabDirectories =
        ["api-jwt-bearer", "api-mini-erp", "ci-delivery", "container-delivery", "git-review", "testing-strategy"];
    private static readonly string ContentRoot = FindContentRoot();
    private static readonly string CatalogRoot = Path.Combine(ContentRoot, "reference");

    [Fact]
    public void FrozenMatrixCoversEveryWeekWithoutAnticipatingS21()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-reference.json"));
        JsonElement[] modules = curriculum.RootElement.GetProperty("modules").EnumerateArray().ToArray();

        Assert.Equal(27, curriculum.RootElement.GetProperty("weeks").GetInt32());
        Assert.Equal(27, modules.Length);
        Assert.Equal(Enumerable.Range(1, 27), modules.Select(module => module.GetProperty("weeks")[0].GetInt32()));

        JsonElement[] incrementModules = modules.Where(module =>
            module.GetProperty("weeks")[0].GetInt32() is >= 11 and <= 20).ToArray();
        Assert.Equal(10, incrementModules.Length);
        // S11 à S13 portent cinq leçons depuis le lot REST — deux sujets de plus par semaine :
        // versionnage et ETag en S11, débit et cache en S12, CORS et webhooks en S13. La semaine
        // 14 en porte cinq aussi depuis le lot JWT.
        Assert.Equal(
            [5, 5, 5, 5, 3, 3, 3, 3, 3, 3],
            incrementModules.Select(module => module.GetProperty("lessonIds").GetArrayLength()));
        // Matrice figée du volume de pratique en S11–S17. Ces semaines partaient toutes de cinq
        // exercices, contre 8,8 par semaine en S1–S10 : l'écart porte précisément sur les semaines
        // qui décident d'une embauche backend. Le lot REST porte S11 à S13 à dix exercices chacune —
        // atteignant et dépassant la cible de huit — et le lot JWT porte S14 à douze.
        Assert.Equal(
            [10, 10, 10, 12, 6, 6, 6],
            incrementModules
                .Where(module => module.GetProperty("weeks")[0].GetInt32() <= 17)
                .Select(module => module.GetProperty("exerciseIds").GetArrayLength()));
        Assert.All(incrementModules.Where(module => module.GetProperty("weeks")[0].GetInt32() >= 18),
            module => Assert.Equal(3, module.GetProperty("exerciseIds").GetArrayLength()));

        string[] lessonIds = incrementModules.SelectMany(module => module.GetProperty("lessonIds").EnumerateArray())
            .Select(value => value.GetString()!).ToArray();
        string[] exerciseIds = incrementModules.SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!).ToArray();
        Assert.Equal(38, lessonIds.Length);
        Assert.Equal(69, exerciseIds.Length);
        Assert.Equal(60, incrementModules.Where(module => module.GetProperty("weeks")[0].GetInt32() <= 17)
            .Sum(module => module.GetProperty("exerciseIds").GetArrayLength()));
        Assert.Equal(9, incrementModules.Where(module => module.GetProperty("weeks")[0].GetInt32() >= 18)
            .Sum(module => module.GetProperty("exerciseIds").GetArrayLength()));

        foreach (JsonElement module in incrementModules)
        {
            int week = module.GetProperty("weeks")[0].GetInt32();
            Assert.Equal(week == 11 ? "week-10" : $"week-{week - 1}",
                Assert.Single(module.GetProperty("prerequisites").EnumerateArray()).GetString());
        }

        Assert.Equal(68, modules.Take(20).SelectMany(module => module.GetProperty("lessonIds").EnumerateArray()).Count());
        Assert.Equal(157, modules.Take(20).SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray()).Count());
        HashSet<string> finalExerciseIds = modules.Skip(20)
            .SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
        // La piste senior (senior-*) vit dans son propre parcours et n'entre pas dans ce releve du
        // socle junior : ses exercices sont exclus du compte des dossiers hors bassin final.
        Assert.Equal(154, Directory.GetDirectories(Path.Combine(CatalogRoot, "exercises"))
            .Select(Path.GetFileName)
            .Count(id => id is not null && !finalExerciseIds.Contains(id)
                && !id.StartsWith("senior-", StringComparison.Ordinal)));
    }

    [Fact]
    public void EveryNewExerciseHasProgressiveAidPrivateTestsAndARealVariant()
    {
        HashSet<string> ids = S11S20ExerciseIds();
        Assert.Equal(69, ids.Count);
        foreach (string id in ids)
        {
            string directory = Path.Combine(CatalogRoot, "exercises", id);
            using JsonDocument manifest = Read(Path.Combine(directory, "exercise.json"));
            JsonElement root = manifest.RootElement;
            Assert.Equal(4, root.GetProperty("hints").GetArrayLength());
            Assert.Equal([1, 2, 3, 4], root.GetProperty("hints").EnumerateArray()
                .Select(hint => hint.GetProperty("level").GetInt32()).ToArray());
            Assert.Contains(root.GetProperty("variantId").GetString()!, ids);
            Assert.True(root.GetProperty("solution").GetProperty("unlock").GetProperty("seriousAttempts").GetInt32() >= 2);
            Assert.Equal(6, root.GetProperty("reflectionFields").GetArrayLength());

            using JsonDocument visible = Read(Path.Combine(directory, "tests", "visible", "cases.json"));
            using JsonDocument hidden = Read(Path.Combine(directory, "tests", "hidden", "cases.json"));
            Assert.InRange(visible.RootElement.GetProperty("cases").GetArrayLength(), 2, 20);
            Assert.InRange(hidden.RootElement.GetProperty("cases").GetArrayLength(), 2, 20);
            Assert.NotEqual(visible.RootElement.GetRawText(), hidden.RootElement.GetRawText());
            Assert.DoesNotContain("NotImplementedException", File.ReadAllText(Path.Combine(directory, "solution", "Submission.cs")), StringComparison.Ordinal);
            Assert.Contains("NotImplementedException", File.ReadAllText(Path.Combine(directory, "starter", "Submission.cs")), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProjectsAndExamsMatchTheS11S20Progression()
    {
        string[] projectIds =
        [
            "project-api-mini-erp-001",
            "project-testing-strategy-001",
            "project-container-delivery-001",
        ];
        foreach (string projectId in projectIds)
        {
            using JsonDocument project = Read(Path.Combine(CatalogRoot, "projects", projectId, "project.json"));
            Assert.Equal("no-complete-solution-before-submission", project.RootElement.GetProperty("solutionPolicy").GetString());
            Assert.Equal(1m, project.RootElement.GetProperty("rubric").EnumerateArray()
                .Sum(item => item.GetProperty("weight").GetDecimal()));
            Assert.True(project.RootElement.GetProperty("milestones").GetArrayLength() >= 4);
            Assert.DoesNotContain("solution complète fournie", File.ReadAllText(Path.Combine(CatalogRoot, "projects", projectId, "brief.md")), StringComparison.OrdinalIgnoreCase);
        }

        using JsonDocument apiExam = Read(Path.Combine(ContentRoot, "exams", "api-security-v1", "exam.json"));
        using JsonDocument testsExam = Read(Path.Combine(ContentRoot, "exams", "tests-quality-v1", "exam.json"));
        Assert.Equal(45, apiExam.RootElement.GetProperty("eligibleExerciseIds").GetArrayLength());
        Assert.Equal(18, testsExam.RootElement.GetProperty("eligibleExerciseIds").GetArrayLength());
        Assert.Equal(8, apiExam.RootElement.GetProperty("drawCount").GetInt32());
        Assert.Equal(8, testsExam.RootElement.GetProperty("drawCount").GetInt32());
        Assert.Equal(80m, apiExam.RootElement.GetProperty("passingScore").GetDecimal());
        Assert.Equal(80m, testsExam.RootElement.GetProperty("passingScore").GetDecimal());
        // L'examen de sécurité tire aussi les cinq exercices OAuth/OIDC de la semaine 21 :
        // son bassin s'étend donc aux modules finals, contrairement à l'examen de tests.
        HashSet<string> securityEligiblePool = S11S20ExerciseIds();
        securityEligiblePool.UnionWith(FinalModuleExerciseIds());
        Assert.All(apiExam.RootElement.GetProperty("eligibleExerciseIds").EnumerateArray(),
            value => Assert.Contains(value.GetString()!, securityEligiblePool));
        Assert.All(testsExam.RootElement.GetProperty("eligibleExerciseIds").EnumerateArray(),
            value => Assert.Contains(value.GetString()!, S11S20ExerciseIds()));
    }

    [Fact]
    public void ApiOpenApiDockerAndCiLabsEncodeTheRequiredBehaviorAndHardening()
    {
        string labs = Path.Combine(ContentRoot, "labs");
        using JsonDocument openApi = Read(Path.Combine(labs, "api-mini-erp", "openapi.json"));
        JsonElement paths = openApi.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/health", out _));
        Assert.True(paths.TryGetProperty("/orders", out JsonElement orders));
        Assert.True(paths.TryGetProperty("/orders/{id}", out JsonElement order));
        Assert.Equal(["200", "400", "401"], orders.GetProperty("get").GetProperty("responses").EnumerateObject().Select(item => item.Name).Order().ToArray());
        Assert.Equal(["201", "400", "401", "403"], orders.GetProperty("post").GetProperty("responses").EnumerateObject().Select(item => item.Name).Order().ToArray());
        Assert.Equal(["200", "401", "404"], order.GetProperty("get").GetProperty("responses").EnumerateObject().Select(item => item.Name).Order().ToArray());

        string dockerfile = File.ReadAllText(Path.Combine(labs, "container-delivery", "Dockerfile"));
        string compose = File.ReadAllText(Path.Combine(labs, "container-delivery", "compose.yaml"));
        Assert.Equal(2, dockerfile.Split("@sha256:", StringSplitOptions.None).Length - 1);
        Assert.Contains("USER $APP_UID", dockerfile, StringComparison.Ordinal);
        foreach (string guard in new[] { "127.0.0.1:", "read_only: true", "cap_drop:", "no-new-privileges:true", "pids_limit:", "mem_limit:", "cpus:", "healthcheck:", "secrets:" })
            Assert.Contains(guard, compose, StringComparison.Ordinal);
        Assert.DoesNotContain("latest", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("privileged: true", compose, StringComparison.OrdinalIgnoreCase);

        string workflow = File.ReadAllText(Path.Combine(labs, "ci-delivery", "workflow.yml"));
        foreach (string proof in new[] { "permissions:", "contents: read", "dotnet restore", "dotnet build", "dotnet test", "docker build", "needs: build-test", "environment: protected-local-rehearsal" })
            Assert.Contains(proof, workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("continue-on-error", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secrets.", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IncrementContainsNoPlaceholderRealSecretOrS21Topic()
    {
        string[] inspected = S11S20LabDirectories
            .SelectMany(lab => Directory.GetFiles(Path.Combine(ContentRoot, "labs", lab), "*", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Path.Combine(CatalogRoot, "curriculum", "lessons"), "*", SearchOption.AllDirectories)
                .Where(path => path.Contains($"{Path.DirectorySeparatorChar}api-", StringComparison.Ordinal)
                    || path.Contains($"{Path.DirectorySeparatorChar}security-", StringComparison.Ordinal)
                    || path.Contains($"{Path.DirectorySeparatorChar}tests-", StringComparison.Ordinal)
                    || path.Contains($"{Path.DirectorySeparatorChar}quality-", StringComparison.Ordinal)
                    || path.Contains($"{Path.DirectorySeparatorChar}git-", StringComparison.Ordinal)
                    || path.Contains($"{Path.DirectorySeparatorChar}docker-", StringComparison.Ordinal)
                    || path.Contains($"{Path.DirectorySeparatorChar}ci-", StringComparison.Ordinal)))
            .Concat(S11S20ExerciseIds().SelectMany(id => Directory.GetFiles(Path.Combine(CatalogRoot, "exercises", id), "*", SearchOption.AllDirectories)))
            .Where(path => Path.GetExtension(path) is ".md" or ".json" or ".cs" or ".yml" or ".yaml" or ".ps1" or ".xml")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string text = string.Join('\n', inspected.Select(File.ReadAllText));
        foreach (string forbidden in new[] { "lorem ipsum", "TODO", "à venir", "change-me", "BEGIN PRIVATE KEY", "ghp_", "sk-live-", "sk-proj-", "Azure App Service", "OpenTelemetry", "Kubernetes", "S21", "S22", "S23", "S24" })
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forge-fake-operator-key", text, StringComparison.Ordinal);
        Assert.Contains("fake", text, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> FinalModuleExerciseIds()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-reference.json"));
        return curriculum.RootElement.GetProperty("modules").EnumerateArray()
            .Where(module => module.GetProperty("weeks")[0].GetInt32() >= 21)
            .SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> S11S20ExerciseIds()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-reference.json"));
        return curriculum.RootElement.GetProperty("modules").EnumerateArray()
            .Where(module => module.GetProperty("weeks")[0].GetInt32() is >= 11 and <= 20)
            .SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
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
