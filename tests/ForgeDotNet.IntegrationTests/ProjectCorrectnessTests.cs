using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Prouve, sans moteur Docker, que chaque suite d'acceptation d'un projet est réellement franchissable
/// et réellement exigeante : la solution de référence passe tous ses cas, le starter en échoue au
/// moins un.
/// </summary>
/// <remarks>
/// Un projet qui déclare des suites sans que personne n'en ait jamais exécuté les cas ne vaudrait pas
/// mieux que la grille d'auto-évaluation qu'il remplace. Et comme la porte A repose désormais sur ces
/// suites, une attente fausse rendrait l'accomplissement impossible ou gratuit.
///
/// Le harnais est celui des exercices — <see cref="LocalExerciseVerifier"/> — parce que le format de
/// suite est le même : un dossier portant <c>tests/runner.json</c>, <c>tests/visible/cases.json</c>
/// et <c>tests/hidden/cases.json</c>. Il ne remplace pas la suite Docker, qui reste la référence pour
/// l'isolation et les quotas.
/// </remarks>
public sealed class ProjectCorrectnessTests
{
    private static readonly string ProjectsRoot =
        Path.Combine(FindRepositoryRoot(), "content", "reference", "projects");

    /// <summary>
    /// Projets dont la réussite peut produire l'accomplissement « mini-projet console vérifié ».
    /// Ce plancher ne peut que monter.
    /// </summary>
    private const int MinimumVerifiableProjects = 4;

    public static TheoryData<string, string> PublishedSuites()
    {
        var data = new TheoryData<string, string>();
        foreach ((string projectId, string suitePath) in EnumerateSuites())
        {
            data.Add(projectId, suitePath);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PublishedSuites))]
    public void ReferenceSolutionPassesEveryVisibleAndHiddenCase(string projectId, string suitePath)
    {
        string directory = Path.Combine(ProjectsRoot, projectId, suitePath);
        ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(directory);

        Assert.Contains(suite.Cases, testCase => testCase.IsVisible);
        Assert.Contains(suite.Cases, testCase => !testCase.IsVisible);

        ExerciseRunOutcome outcome = LocalExerciseVerifier.Run(
            File.ReadAllText(Path.Combine(ProjectsRoot, projectId, "solution", "Submission.cs")),
            suite,
            $"{projectId}-{suitePath}-solution");

        Assert.True(
            outcome.Compiled,
            $"{projectId}/{suitePath} : la solution de référence ne compile pas." + Environment.NewLine
            + string.Join(Environment.NewLine, outcome.CompilerErrors));
        Assert.True(
            outcome.FailedCases.Count == 0,
            $"{projectId}/{suitePath} : la solution échoue sur {outcome.FailedCases.Count} cas."
            + Environment.NewLine + string.Join(Environment.NewLine, outcome.FailedCases));
    }

    [Theory]
    [MemberData(nameof(PublishedSuites))]
    public void StarterCompilesButFailsAtLeastOneCase(string projectId, string suitePath)
    {
        string directory = Path.Combine(ProjectsRoot, projectId, suitePath);
        ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(directory);

        ExerciseRunOutcome outcome = LocalExerciseVerifier.Run(
            File.ReadAllText(Path.Combine(ProjectsRoot, projectId, "starter", "Submission.cs")),
            suite,
            $"{projectId}-{suitePath}-starter");

        Assert.True(
            outcome.Compiled,
            $"{projectId} : le starter ne compile pas, l'apprenant partirait d'un état invalide."
            + Environment.NewLine + string.Join(Environment.NewLine, outcome.CompilerErrors));
        Assert.True(
            outcome.FailedCases.Count > 0,
            $"{projectId}/{suitePath} : le starter passe déjà tous les cas, le jalon ne prouve rien.");
    }

    /// <summary>
    /// Deux cas portant les mêmes arguments exécutent deux fois la même chose.
    /// </summary>
    [Fact]
    public void NoSuiteRepeatsTheSameArguments()
    {
        var duplicates = new List<string>();
        foreach ((string projectId, string suitePath) in EnumerateSuites())
        {
            ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(
                Path.Combine(ProjectsRoot, projectId, suitePath));
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ExerciseCase testCase in suite.Cases)
            {
                string key = System.Text.Json.Nodes.JsonNode
                    .Parse(testCase.Arguments.GetRawText())?.ToJsonString() ?? "null";
                if (seen.TryGetValue(key, out string? first))
                {
                    duplicates.Add(
                        $"{projectId}/{suitePath} : « {testCase.Name} » répète les arguments de « {first} ».");
                }
                else
                {
                    seen.Add(key, testCase.Name);
                }
            }
        }

        Assert.True(
            duplicates.Count == 0,
            "Des cas répètent les mêmes arguments :" + Environment.NewLine
            + string.Join(Environment.NewLine, duplicates));
    }

    /// <summary>
    /// Chaque suite déclarée correspond à un jalon déclaré, et chaque jalon d'un projet vérifiable
    /// porte sa suite.
    /// </summary>
    /// <remarks>
    /// Une suite orpheline évaluerait un contrat que le brief n'annonce pas ; un jalon sans suite
    /// laisserait croire qu'il est mesuré alors qu'il ne l'est pas. C'est ce dernier cas qui a produit
    /// le défaut d'origine : neuf projets annonçaient des jalons que rien n'exécutait.
    /// </remarks>
    [Fact]
    public void EveryVerifiableProjectPairsItsMilestonesWithItsSuites()
    {
        var offenders = new List<string>();
        int verifiable = 0;

        foreach (string directory in Directory.GetDirectories(ProjectsRoot)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            using JsonDocument manifest = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(directory, "project.json")));
            if (!manifest.RootElement.TryGetProperty("acceptanceSuites", out JsonElement suites))
            {
                continue;
            }

            verifiable++;
            string projectId = Path.GetFileName(directory);
            string[] milestones = manifest.RootElement.GetProperty("milestones").EnumerateArray()
                .Select(item => item.GetProperty("id").GetString()!)
                .ToArray();
            string[] covered = suites.EnumerateArray()
                .Select(item => item.GetProperty("milestoneId").GetString()!)
                .ToArray();

            foreach (string milestone in milestones.Except(covered, StringComparer.Ordinal))
            {
                offenders.Add($"{projectId} : le jalon « {milestone} » n'est mesuré par aucune suite.");
            }

            foreach (string orphan in covered.Except(milestones, StringComparer.Ordinal))
            {
                offenders.Add($"{projectId} : la suite « {orphan} » ne correspond à aucun jalon.");
            }

            if (!File.Exists(Path.Combine(directory, "starter", "Submission.cs"))
                || !File.Exists(Path.Combine(directory, "solution", "Submission.cs")))
            {
                offenders.Add($"{projectId} : starter ou solution de référence absent.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
        Assert.True(
            verifiable >= MinimumVerifiableProjects,
            $"{verifiable} projets vérifiables, contre {MinimumVerifiableProjects} attendus.");
    }

    /// <summary>
    /// La clé d'exigence déclarée par un projet doit être exigée par une porte, et tout projet qui
    /// en déclare une doit être réellement vérifiable.
    /// </summary>
    /// <remarks>
    /// Sans ce contrôle, un projet pourrait annoncer satisfaire une exigence qu'aucune porte ne
    /// connaît — l'accomplissement serait produit et n'ouvrirait rien — ou l'annoncer sans porter la
    /// moindre suite, ce qui reviendrait à la déclaration manuelle que la politique refuse.
    /// </remarks>
    [Fact]
    public void DeclaredAchievementKeysAreRequiredBySomeGateAndBackedBySuites()
    {
        string[] gateKeys = ForgeDotNet.Domain.Mastery.MasteryPolicyCatalog.Version1.Gates
            .SelectMany(gate => gate.Requirements)
            .Where(requirement => requirement.AchievementKey is not null)
            .Select(requirement => requirement.AchievementKey!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var offenders = new List<string>();
        var declared = new List<string>();

        foreach (string directory in Directory.GetDirectories(ProjectsRoot)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            using JsonDocument manifest = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(directory, "project.json")));
            if (!manifest.RootElement.TryGetProperty("achievementKey", out JsonElement key))
            {
                continue;
            }

            string projectId = Path.GetFileName(directory);
            string value = key.GetString()!;
            declared.Add(value);
            if (!gateKeys.Contains(value, StringComparer.Ordinal))
            {
                offenders.Add($"{projectId} : la clé « {value} » n'est exigée par aucune porte.");
            }

            if (!manifest.RootElement.TryGetProperty("acceptanceSuites", out _))
            {
                offenders.Add($"{projectId} : déclare une clé sans porter la moindre suite d'acceptation.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));

        // La porte A repose sur cette clé : si plus aucun projet ne la déclare, elle redevient
        // infranchissable et personne ne le verrait.
        Assert.Contains(
            ForgeDotNet.Domain.Mastery.MasteryPolicyCatalog.ConsoleProject,
            declared,
            StringComparer.Ordinal);
    }

    private static IEnumerable<(string ProjectId, string SuitePath)> EnumerateSuites()
    {
        foreach (string directory in Directory.GetDirectories(ProjectsRoot)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            string manifestPath = Path.Combine(directory, "project.json");
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!manifest.RootElement.TryGetProperty("acceptanceSuites", out JsonElement suites))
            {
                continue;
            }

            foreach (JsonElement suite in suites.EnumerateArray())
            {
                yield return (
                    Path.GetFileName(directory),
                    suite.GetProperty("suitePath").GetString()!.TrimEnd('/'));
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Racine du dépôt de test introuvable.");
    }
}
