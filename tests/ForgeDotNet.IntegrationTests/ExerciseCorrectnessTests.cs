using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Prouve, sans moteur Docker, que chaque exercice publié est réellement résoluble et réellement
/// exigeant : la solution passe tous ses cas, le starter en échoue au moins un.
/// </summary>
/// <remarks>
/// Jusqu'ici, cette preuve n'existait que dans <c>InitialCSharpContentTests</c>, qui exige Docker.
/// Un environnement sans moteur ne pouvait donc pas distinguer un exercice correct d'un exercice
/// dont personne n'avait jamais exécuté les tests. Ce test comble ce trou ; il ne remplace pas la
/// suite Docker, qui reste la référence pour l'isolation et les quotas.
/// </remarks>
public sealed class ExerciseCorrectnessTests
{
    private static readonly string CatalogRoot =
        Path.Combine(FindRepositoryRoot(), "content", "reference", "exercises");

    public static TheoryData<string> PublishedExercises()
    {
        var data = new TheoryData<string>();
        foreach (string directory in Directory.GetDirectories(CatalogRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(directory));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PublishedExercises))]
    public void PublishedSolutionPassesEveryVisibleAndHiddenCase(string exerciseId)
    {
        string directory = Path.Combine(CatalogRoot, exerciseId);
        ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(directory);

        // Le volume de cas est contrôlé séparément par EveryExerciseCarriesEnoughCases : ici, on
        // vérifie seulement que les deux familles existent avant de juger la correction.
        Assert.Contains(suite.Cases, testCase => testCase.IsVisible);
        Assert.Contains(suite.Cases, testCase => !testCase.IsVisible);

        ExerciseRunOutcome outcome = LocalExerciseVerifier.Run(
            File.ReadAllText(Path.Combine(directory, "solution", "Submission.cs")),
            suite,
            $"{exerciseId}-solution");

        Assert.True(
            outcome.Compiled,
            $"{exerciseId} : la solution ne compile pas." + Environment.NewLine
            + string.Join(Environment.NewLine, outcome.CompilerErrors));
        Assert.True(
            outcome.FailedCases.Count == 0,
            $"{exerciseId} : la solution échoue sur {outcome.FailedCases.Count} cas." + Environment.NewLine
            + string.Join(Environment.NewLine, outcome.FailedCases));
    }

    /// <summary>
    /// Un starter qui passerait déjà les tests rendrait l'exercice sans objet : la maîtrise
    /// mesurée ne prouverait rien.
    /// </summary>
    [Theory]
    [MemberData(nameof(PublishedExercises))]
    public void PublishedStarterCompilesButFailsAtLeastOneCase(string exerciseId)
    {
        string directory = Path.Combine(CatalogRoot, exerciseId);
        ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(directory);

        ExerciseRunOutcome outcome = LocalExerciseVerifier.Run(
            File.ReadAllText(Path.Combine(directory, "starter", "Submission.cs")),
            suite,
            $"{exerciseId}-starter");

        Assert.True(
            outcome.Compiled,
            $"{exerciseId} : le starter ne compile pas, l'apprenant partirait d'un état invalide."
            + Environment.NewLine + string.Join(Environment.NewLine, outcome.CompilerErrors));
        Assert.True(
            outcome.FailedCases.Count > 0,
            $"{exerciseId} : le starter passe déjà tous les cas, l'exercice ne prouve rien.");
    }

    /// <summary>
    /// Deux cas portant les mêmes arguments exécutent deux fois la même chose : ils gonflent le
    /// compte sans réfuter une implémentation supplémentaire.
    /// </summary>
    /// <remarks>
    /// Sans ce contrôle, atteindre un volume de cas serait trivial et le volume cesserait d'être un
    /// indicateur de couverture — c'est exactement le défaut que la reprise du contenu combat.
    /// </remarks>
    [Fact]
    public void NoExerciseRepeatsTheSameArguments()
    {
        var duplicates = new List<string>();
        foreach (string directory in Directory.GetDirectories(CatalogRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(directory);
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ExerciseCase testCase in suite.Cases)
            {
                // Comparaison sur la forme normalisée : le texte brut porte l'indentation du
                // fichier, et deux cas identiques écrits différemment passeraient inaperçus.
                string key = System.Text.Json.Nodes.JsonNode
                    .Parse(testCase.Arguments.GetRawText())?.ToJsonString() ?? "null";
                if (seen.TryGetValue(key, out string? first))
                {
                    duplicates.Add(
                        $"{Path.GetFileName(directory)} : « {testCase.Name} » répète les arguments de « {first} ».");
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
    /// Cliquet sur le volume de cas, sur le modèle du registre de dette éditoriale.
    /// </summary>
    /// <remarks>
    /// Cent vingt-cinq exercices issus du générateur ne portaient que deux cas visibles et deux
    /// cachés, contre trois et quatre pour les dix relus à la main : une implémentation fausse avait
    /// deux fois moins de chances d'être réfutée. La dette est aujourd'hui close, d'où un plafond à
    /// zéro. Tout exercice neuf doit donc naître à trois et quatre.
    ///
    /// Exception assumée : un exercice dont tous les paramètres sont booléens n'admet que deux
    /// puissance n entrées distinctes. Les couvrir toutes vaut mieux que d'atteindre un volume en
    /// répétant des arguments, ce que <see cref="NoExerciseRepeatsTheSameArguments"/> interdit.
    /// </remarks>
    [Fact]
    public void EveryExerciseCarriesEnoughCases()
    {
        const int UnderCoveredCeiling = 0;

        var underCovered = new List<string>();
        foreach (string directory in Directory.GetDirectories(CatalogRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            ExerciseSuite suite = LocalExerciseVerifier.LoadSuite(directory);
            int visible = suite.Cases.Count(testCase => testCase.IsVisible);
            int hidden = suite.Cases.Count - visible;
            if ((visible < 3 || hidden < 4) && !CoversWholeDomain(suite))
            {
                underCovered.Add($"{Path.GetFileName(directory)} ({visible} visibles, {hidden} cachés)");
            }
        }

        Assert.True(
            underCovered.Count <= UnderCoveredCeiling,
            $"{underCovered.Count} exercices sous-couverts pour un plafond de {UnderCoveredCeiling}."
            + Environment.NewLine + string.Join(Environment.NewLine, underCovered.Take(10)));
    }

    /// <summary>
    /// Un exercice à domaine d'entrée entièrement booléen doit pointer, par sa variante, vers un
    /// exercice à domaine ouvert — et ce frère doit pointer en retour.
    /// </summary>
    /// <remarks>
    /// Douze exercices booléens formaient des chaînes de variantes fermées entre eux : la variante,
    /// censée offrir une transposition, renvoyait vers une autre table de vérité mémorisable. La
    /// paire réciproque booléen-ouvert garantit que chaque décision compacte a son exercice
    /// d'analyse sur le même sujet, et qu'aucun ajout futur ne recrée une chaîne fermée.
    /// </remarks>
    [Fact]
    public void EveryBooleanDomainExerciseVariesIntoAReciprocalOpenDomainSibling()
    {
        var offenders = new List<string>();
        foreach (string directory in Directory.GetDirectories(CatalogRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            string id = Path.GetFileName(directory);
            if (!HasBooleanOnlyDomain(id))
            {
                continue;
            }

            string variantId = VariantOf(id);
            if (HasBooleanOnlyDomain(variantId))
            {
                offenders.Add($"{id} : sa variante « {variantId} » est aussi à domaine booléen.");
                continue;
            }

            if (!string.Equals(VariantOf(variantId), id, StringComparison.Ordinal))
            {
                offenders.Add($"{id} : son frère ouvert « {variantId} » ne pointe pas en retour.");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Des exercices booléens n'ont pas leur paire réciproque à domaine ouvert :"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static bool HasBooleanOnlyDomain(string exerciseId)
    {
        using JsonDocument runner = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(CatalogRoot, exerciseId, "tests", "runner.json")));
        string[] parameterTypes = runner.RootElement.GetProperty("parameterTypes").EnumerateArray()
            .Select(item => item.GetString()!).ToArray();
        return parameterTypes.Length > 0
            && parameterTypes.All(type => string.Equals(type, "bool", StringComparison.Ordinal));
    }

    private static string VariantOf(string exerciseId)
    {
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(CatalogRoot, exerciseId, "exercise.json")));
        return manifest.RootElement.GetProperty("variantId").GetString()!;
    }

    /// <summary>
    /// Vrai lorsque la suite épuise un domaine d'entrée fini — seuls les paramètres booléens en
    /// définissent un, avec deux puissance n combinaisons.
    /// </summary>
    private static bool CoversWholeDomain(ExerciseSuite suite)
    {
        if (suite.ParameterTypes.Count == 0
            || !suite.ParameterTypes.All(type => string.Equals(type, "bool", StringComparison.Ordinal)))
        {
            return false;
        }

        return suite.Cases.Count >= 1 << suite.ParameterTypes.Count;
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
