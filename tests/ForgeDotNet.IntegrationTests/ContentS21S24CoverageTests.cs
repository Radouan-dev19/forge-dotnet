using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForgeDotNet.IntegrationTests;

[Trait("Category", "ContentS21S24")]
public sealed class ContentS21S24CoverageTests
{
    private static readonly string ContentRoot = FindContentRoot();
    private static readonly string CatalogRoot = Path.Combine(ContentRoot, "reference");

    [Fact]
    public void FinalMatrixCoversTwentyFourWeeksWithExactUsefulVolumes()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-reference.json"));
        JsonElement[] modules = curriculum.RootElement.GetProperty("modules").EnumerateArray().ToArray();

        Assert.Equal(27, curriculum.RootElement.GetProperty("weeks").GetInt32());
        Assert.Equal(5, curriculum.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(Enumerable.Range(1, 27), modules.Select(Week));
        Assert.All(modules.Skip(1), module =>
            Assert.Equal($"week-{Week(module) - 1}", Assert.Single(module.GetProperty("prerequisites").EnumerateArray()).GetString()));

        JsonElement[] finalModules = modules.Where(module => Week(module) is >= 21 and <= 24).ToArray();
        // La semaine 21 porte, en plus de son socle Azure, le lot OAuth/OIDC : trois leçons
        // et cinq exercices security-*, l'identité gérée étant un flux d'identifiants client.
        Assert.Equal([6, 3, 2, 2], finalModules.Select(module => module.GetProperty("lessonIds").GetArrayLength()));
        Assert.Equal([7, 3, 1, 3], finalModules.Select(module => module.GetProperty("exerciseIds").GetArrayLength()));
        // Les familles admises dans les semaines finales sont celles que ces semaines enseignent :
        // Azure et sécurité en S21, l'observabilité en S22, l'anglais professionnel en S24. La règle
        // existe pour empêcher qu'un exercice hors sujet s'y range, pas pour figer un préfixe.
        Assert.All(finalModules.SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray()),
            id => Assert.True(
                id.GetString()!.StartsWith("azure-", StringComparison.Ordinal)
                    || id.GetString()!.StartsWith("security-", StringComparison.Ordinal)
                    || id.GetString()!.StartsWith("observability-", StringComparison.Ordinal)
                    || id.GetString()!.StartsWith("english-", StringComparison.Ordinal),
                $"Exercice final hors familles attendues : {id.GetString()}"));

        Assert.Equal(96, Directory.GetFiles(Path.Combine(CatalogRoot, "curriculum", "lessons"), "lesson.json", SearchOption.AllDirectories).Length);
        Assert.Equal(187, Directory.GetFiles(Path.Combine(CatalogRoot, "exercises"), "exercise.json", SearchOption.AllDirectories).Length);
        Assert.Equal(29, Directory.GetFiles(Path.Combine(CatalogRoot, "debugging"), "scenario.json", SearchOption.AllDirectories).Length);
        Assert.Equal(242, Directory.GetFiles(Path.Combine(CatalogRoot, "interviews"), "*.json", SearchOption.TopDirectoryOnly).Length);
        Assert.Equal(51, Directory.GetFiles(Path.Combine(CatalogRoot, "english"), "*.json", SearchOption.TopDirectoryOnly).Length);
        // Un projet porte désormais un dossier, comme un exercice : son manifeste s'appelle
        // project.json et ses suites d'acceptation vivent à côté.
        Assert.Equal(16, Directory.GetDirectories(Path.Combine(CatalogRoot, "projects")).Length);
        Assert.Equal(9, Directory.GetFiles(Path.Combine(ContentRoot, "exams"), "exam.json", SearchOption.AllDirectories).Length);
        // La banque de cartes de révision ajoute un fichier au catalogue : c'est la seule source
        // de rétention espacée qui survive à l'expiration des preuves du bilan d'entrée.
        Assert.Single(Directory.GetFiles(Path.Combine(CatalogRoot, "reviews"), "*.json", SearchOption.TopDirectoryOnly));
        // Instantané de volume, pas un plancher. Le 18 août 2026, six projets vérifiables — les
        // producteurs des clés validation-errors, logs, incident.simulated, performance, security et
        // feature.autonomous — ajoutent chacun treize fichiers : manifeste, brief, squelette,
        // solution de référence et trois suites d'acceptation.
        Assert.Equal(2_771, Directory.GetFiles(CatalogRoot, "*", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void FrontEndBlockHasExpectedVolumesAndBuildableContracts()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-reference.json"));
        JsonElement[] frontModules = curriculum.RootElement.GetProperty("modules").EnumerateArray()
            .Where(module => Week(module) is >= 25 and <= 27).ToArray();
        // Bloc front-end dédié (S25–S27) : quatre leçons de socle et deux exercices en S25, deux
        // leçons de framework et un exercice en S26, la leçon Blazor et un exercice en S27.
        Assert.Equal(3, frontModules.Length);
        Assert.Equal([4, 2, 1], frontModules.Select(module => module.GetProperty("lessonIds").GetArrayLength()));
        Assert.Equal([2, 1, 1], frontModules.Select(module => module.GetProperty("exerciseIds").GetArrayLength()));

        string[] exerciseIds = frontModules.SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!).ToArray();
        Assert.Equal(4, exerciseIds.Length);
        Assert.All(exerciseIds, id => Assert.StartsWith("front-", id));

        foreach (string id in exerciseIds)
        {
            string directory = Path.Combine(CatalogRoot, "exercises", id);
            using JsonDocument manifest = Read(Path.Combine(directory, "exercise.json"));
            JsonElement root = manifest.RootElement;
            Assert.Equal([1, 2, 3, 4], root.GetProperty("hints").EnumerateArray()
                .Select(hint => hint.GetProperty("level").GetInt32()).ToArray());
            Assert.Equal(6, root.GetProperty("reflectionFields").GetArrayLength());
            Assert.Contains(root.GetProperty("variantId").GetString()!, exerciseIds);
            Assert.True(Read(Path.Combine(directory, "tests", "visible", "cases.json"))
                .RootElement.GetProperty("cases").GetArrayLength() >= 3);
            Assert.True(Read(Path.Combine(directory, "tests", "hidden", "cases.json"))
                .RootElement.GetProperty("cases").GetArrayLength() >= 4);
            Assert.Contains("NotImplementedException", File.ReadAllText(Path.Combine(directory, "starter", "Submission.cs")), StringComparison.Ordinal);
            Assert.DoesNotContain("NotImplementedException", File.ReadAllText(Path.Combine(directory, "solution", "Submission.cs")), StringComparison.Ordinal);
        }

        string[] lessonIds = frontModules.SelectMany(module => module.GetProperty("lessonIds").EnumerateArray())
            .Select(value => value.GetString()!).ToArray();
        Assert.Equal(7, lessonIds.Length);
        Assert.All(lessonIds, id => Assert.True(
            File.Exists(Path.Combine(CatalogRoot, "curriculum", "lessons", id, "lesson.json")),
            $"Leçon front-end absente : {id}"));
    }

    [Fact]
    public void FinalWeekActivitiesHaveProgressiveAidPrivateProofsAndBuildableContracts()
    {
        string[] ids = S21S24ExerciseIds();
        Assert.Equal(14, ids.Length);

        foreach (string id in ids)
        {
            string directory = Path.Combine(CatalogRoot, "exercises", id);
            using JsonDocument manifest = Read(Path.Combine(directory, "exercise.json"));
            JsonElement root = manifest.RootElement;
            Assert.Equal(4, root.GetProperty("hints").GetArrayLength());
            Assert.Equal([1, 2, 3, 4], root.GetProperty("hints").EnumerateArray()
                .Select(hint => hint.GetProperty("level").GetInt32()).ToArray());
            Assert.Contains(root.GetProperty("variantId").GetString()!, ids);
            string interviewId = root.GetProperty("interviewQuestionId").GetString()!;
            Assert.True(
                interviewId.StartsWith("interview-azure-", StringComparison.Ordinal)
                    || interviewId.StartsWith("interview-security-", StringComparison.Ordinal)
                    || interviewId.StartsWith("interview-observability-", StringComparison.Ordinal)
                    || interviewId.StartsWith("interview-english-", StringComparison.Ordinal),
                $"{id} : fiche d'entretien hors familles attendues ({interviewId}).");
            // Contrat de CONTENT_AUTHORING_STANDARD : trois cas visibles et quatre cachés, sauf
            // lorsque tous les paramètres sont booléens — le domaine ne compte alors que deux
            // puissance n entrées, et les couvrir toutes vaut mieux que de répéter des arguments.
            int visible = Read(Path.Combine(directory, "tests", "visible", "cases.json"))
                .RootElement.GetProperty("cases").GetArrayLength();
            int hidden = Read(Path.Combine(directory, "tests", "hidden", "cases.json"))
                .RootElement.GetProperty("cases").GetArrayLength();
            string[] parameterTypes = Read(Path.Combine(directory, "tests", "runner.json"))
                .RootElement.GetProperty("parameterTypes").EnumerateArray()
                .Select(type => type.GetString()!).ToArray();
            bool booleanDomain = parameterTypes.Length > 0
                && parameterTypes.All(type => string.Equals(type, "bool", StringComparison.Ordinal));

            Assert.True(
                (visible >= 3 && hidden >= 4)
                || (booleanDomain && visible + hidden >= 1 << parameterTypes.Length),
                $"{id} : couverture insuffisante ({visible} visibles, {hidden} cachés).");
            Assert.Contains("NotImplementedException", File.ReadAllText(Path.Combine(directory, "starter", "Submission.cs")), StringComparison.Ordinal);
            Assert.DoesNotContain("NotImplementedException", File.ReadAllText(Path.Combine(directory, "solution", "Submission.cs")), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void InterviewAndEnglishLotsHaveExactDistributionAndDistinctPractice()
    {
        JsonElement[] interviews = Directory.GetFiles(Path.Combine(CatalogRoot, "interviews"), "*.json")
            .Select(path => Read(path).RootElement.Clone()).ToArray();
        Assert.Equal(136, interviews.Count(item => item.GetProperty("level").GetString() == "junior"));
        Assert.Equal(71, interviews.Count(item => item.GetProperty("level").GetString() == "intermediate"));
        Assert.Equal(35, interviews.Count(item => item.GetProperty("level").GetString() == "advanced"));
        JsonElement[] newInterviews = interviews.Where(item =>
            item.GetProperty("id").GetString()!.StartsWith("interview-s21-s24-", StringComparison.Ordinal)
            || item.GetProperty("id").GetString()!.StartsWith("interview-azure-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(62, newInterviews.Length);
        Assert.Equal(62, newInterviews.Select(item => item.GetProperty("question").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.All(newInterviews, item => Assert.True(item.GetProperty("observableCriteria").GetArrayLength() >= 2));

        JsonElement[] cards = Directory.GetFiles(Path.Combine(CatalogRoot, "english"), "english-card-*.json")
            .Select(path => Read(path).RootElement.Clone()).ToArray();
        Assert.Equal(50, cards.Length);
        Assert.Equal(25, cards.Count(item => item.GetProperty("id").GetString()!.EndsWith("-written", StringComparison.Ordinal)));
        Assert.Equal(25, cards.Count(item => item.GetProperty("id").GetString()!.EndsWith("-spoken", StringComparison.Ordinal)));
        Assert.Equal(50, cards.Select(item => item.GetProperty("situation").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.All(cards, item =>
        {
            Assert.True(item.GetProperty("vocabulary").GetArrayLength() >= 2);
            Assert.True(item.GetProperty("expectedElements").GetArrayLength() >= 3);
            Assert.Contains("evidence", item.GetProperty("modelAnswer").GetString()!, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void FinalProjectIsGuidedButNeverSuppliedAndItsRubricIsComplete()
    {
        const string id = "project-final-service-operations-001";
        string projectDirectory = Path.Combine(CatalogRoot, "projects", id);
        string manifestPath = Path.Combine(projectDirectory, "project.json");
        using JsonDocument project = Read(manifestPath);
        JsonElement root = project.RootElement;

        Assert.Equal([21, 22, 23, 24], root.GetProperty("weeks").EnumerateArray().Select(item => item.GetInt32()).ToArray());
        Assert.Equal("no-complete-solution-before-submission", root.GetProperty("solutionPolicy").GetString());
        Assert.Equal(5, root.GetProperty("milestones").GetArrayLength());
        Assert.Equal(1m, root.GetProperty("rubric").EnumerateArray().Sum(item => item.GetProperty("weight").GetDecimal()));
        Assert.All(root.GetProperty("milestones").EnumerateArray(), milestone =>
        {
            Assert.False(string.IsNullOrWhiteSpace(milestone.GetProperty("evidence").GetString()));
            Assert.True(milestone.GetProperty("acceptanceCriteria").GetArrayLength() >= 3);
        });
        // Le projet final reste guidé sans être fourni : ni squelette, ni corrigé, ni suite
        // d'acceptation qui dicterait sa forme. C'est une soutenance, pas un exercice long.
        Assert.False(Directory.Exists(Path.Combine(projectDirectory, "starter")));
        Assert.False(Directory.Exists(Path.Combine(projectDirectory, "solution")));
        Assert.False(root.TryGetProperty("acceptanceSuites", out _));
        string brief = File.ReadAllText(Path.Combine(projectDirectory, root.GetProperty("briefPath").GetString()!));
        Assert.Contains("Aucun squelette métier", brief, StringComparison.Ordinal);
        Assert.Contains("mode simulé", brief, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("public static class Submission", brief, StringComparison.Ordinal);
    }

    [Fact]
    public void ExamsSevenAndEightUseExistingExercisesAndKeepPrivateAnswersOutsideBanks()
    {
        string[] directories = ["azure-observability-v1", "final-readiness-v1"];
        HashSet<string> exerciseIds = Directory.GetDirectories(Path.Combine(CatalogRoot, "exercises"))
            .Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal)!;
        foreach (string directory in directories)
        {
            string examDirectory = Path.Combine(ContentRoot, "exams", directory);
            using JsonDocument exam = Read(Path.Combine(examDirectory, "exam.json"));
            string[] candidates = exam.RootElement.GetProperty("eligibleExerciseIds").EnumerateArray()
                .Select(item => item.GetString()!).ToArray();
            Assert.Equal(8, exam.RootElement.GetProperty("drawCount").GetInt32());
            Assert.Equal(80m, exam.RootElement.GetProperty("passingScore").GetDecimal());
            // Volumes figés des deux banques finales : la première accueille les deux exercices
            // d'anglais professionnel de la semaine 24, la seconde l'exercice d'échantillonnage
            // de la semaine 22.
            (int minCandidates, int maxCandidates) = directory == "final-readiness-v1" ? (22, 22) : (27, 27);
            Assert.InRange(candidates.Length, minCandidates, maxCandidates);
            Assert.Equal(candidates.Length, candidates.Distinct(StringComparer.Ordinal).Count());
            Assert.All(candidates, id => Assert.Contains(id, exerciseIds));
            Assert.Single(Directory.GetFiles(examDirectory, "*", SearchOption.AllDirectories));
        }
    }

    [Fact]
    public void AzureAndCareerLabsAreOptionalBoundedAndContainNoCommittedCredential()
    {
        string lab = Path.Combine(ContentRoot, "labs", "azure-operations");
        string bicep = File.ReadAllText(Path.Combine(lab, "infra", "main.bicep"));
        foreach (string proof in new[]
        {
            "Microsoft.Web/sites@", "Microsoft.App/containerApps@", "Microsoft.Sql/servers@",
            "Microsoft.Storage/storageAccounts@", "Microsoft.KeyVault/vaults@",
            "Microsoft.Insights/components@", "SystemAssigned", "allowSharedKeyAccess: false",
            "publicNetworkAccess: 'Disabled'",
        })
        {
            Assert.Contains(proof, bicep, StringComparison.Ordinal);
        }

        string inspected = string.Join('\n', Directory.GetFiles(lab, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText));
        foreach (string forbidden in new[] { "lorem ipsum", "TODO", "change-me" })
        {
            Assert.DoesNotContain(forbidden, inspected, StringComparison.OrdinalIgnoreCase);
        }
        foreach (string credentialPattern in new[]
        {
            "-----BEGIN (?:RSA |EC )?PRIVATE KEY-----",
            "AccountKey=[A-Za-z0-9+/]{20,}",
            "SharedAccessSignature=[^'\\s]{20,}",
            "ghp_[A-Za-z0-9]{20,}",
            "sk-(?:live|proj)-[A-Za-z0-9_-]{10,}",
        })
        {
            Assert.DoesNotMatch(new Regex(credentialPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), inspected);
        }
        Assert.Contains("peut être facturé", inspected, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aucune ressource Azure créée", inspected, StringComparison.OrdinalIgnoreCase);

        string career = Path.Combine(CatalogRoot, "career");
        foreach (string file in new[] { "CV-EVIDENCE.md", "STAR-WORKBOOK.md", "APPLICATION-TRACKER.md", "NEGOTIATION-GUIDE.md", "POST-HIRE-PLAN.md", "Export-CareerEvidence.ps1" })
        {
            Assert.True(File.Exists(Path.Combine(career, file)), $"Support carrière absent : {file}");
        }
        string careerText = string.Join('\n', Directory.GetFiles(career).Select(File.ReadAllText));
        Assert.Contains("données personnelles", careerText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ne promet", careerText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nʼimplémente pas un parcours post-embauche", careerText, StringComparison.OrdinalIgnoreCase);
    }

    private static int Week(JsonElement module) => module.GetProperty("weeks")[0].GetInt32();

    private static string[] S21S24ExerciseIds()
    {
        using JsonDocument curriculum = Read(Path.Combine(CatalogRoot, "curriculum", "forge-reference.json"));
        return curriculum.RootElement.GetProperty("modules").EnumerateArray()
            .Where(module => Week(module) is >= 21 and <= 24)
            .SelectMany(module => module.GetProperty("exerciseIds").EnumerateArray())
            .Select(value => value.GetString()!)
            .ToArray();
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
