using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Refuse le retour d'une échelle d'indices générique sur les cent trente-cinq exercices publiés.
/// </summary>
/// <remarks>
/// L'indice de niveau 4 est la dernière marche avant le déverrouillage de la solution. Treize
/// formulations couvraient les cent trente-cinq exercices, dont une seule pour soixante-quinze
/// d'entre eux. Un indice générique ne débloque personne : il envoie l'apprenant vers la solution,
/// ce que <c>docs/MASTERY.md</c> sanctionne par une pratique à zéro et par l'exclusion de
/// l'exercice des comptes autonomes. Le texte cloné convertit donc « bloqué » en « 0 ».
///
/// Ces tests figent l'état atteint : chaque exercice porte quatre indices distincts, aucun texte
/// d'indice ni jeu d'erreurs fréquentes n'est partagé par plus de trois exercices — le seuil de la
/// règle <c>cloned-content</c> de <c>docs/CONTENT_AUTHORING_STANDARD.md</c> — et aucun indice ne
/// recopie une ligne de la solution, ce qui rendrait le plafond de score à 60 équivalent à un
/// déverrouillage gratuit.
/// </remarks>
public sealed class ExerciseHintQualityTests
{
    /// <summary>
    /// Seuil de la règle <c>cloned-content</c> : un texte partagé par plus de trois documents du lot
    /// est un texte recopié. Appliqué ici aux deux champs qui portent l'aide réelle.
    /// </summary>
    private const int MaximumSharingExercises = 3;

    /// <summary>
    /// Longueur minimale d'une ligne de solution prise en compte dans le contrôle de recopie. En
    /// dessous, la ligne est une accolade ou un en-tête de classe : elle ne porte aucune
    /// information et sa présence dans un indice ne prouve rien.
    /// </summary>
    private const int MinimumMeaningfulSolutionLineLength = 20;

    private static readonly string ExercisesRoot = Path.Combine(
        FindRepositoryRoot(), "content", "reference", "exercises");

    [Fact]
    public void EveryExerciseCarriesFourDistinctHints()
    {
        var offenders = new List<string>();

        foreach (Exercise exercise in ReadExercises())
        {
            if (exercise.Hints.Count != 4)
            {
                offenders.Add($"{exercise.Id} : {exercise.Hints.Count} indice(s) au lieu de quatre.");
                continue;
            }

            int distinct = exercise.Hints.Distinct(StringComparer.Ordinal).Count();
            if (distinct != exercise.Hints.Count)
            {
                offenders.Add($"{exercise.Id} : deux indices portent le même texte.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void NoHintTextIsSharedByMoreThanThreeExercises(int level)
    {
        Dictionary<string, List<string>> byText = ReadExercises()
            .Where(exercise => exercise.Hints.Count >= level)
            .GroupBy(exercise => exercise.Hints[level - 1], StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(exercise => exercise.Id).OrderBy(id => id, StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);

        string[] shared = byText
            .Where(pair => pair.Value.Count > MaximumSharingExercises)
            .Select(pair => $"« {Truncate(pair.Key)} » partagé par {string.Join(", ", pair.Value)}")
            .ToArray();

        Assert.True(
            shared.Length == 0,
            $"Indice de niveau {level} recopié :\n{string.Join('\n', shared)}");
    }

    [Fact]
    public void NoCommonMistakeSetIsSharedByMoreThanThreeExercises()
    {
        string[] shared = ReadExercises()
            .GroupBy(exercise => string.Join(" | ", exercise.CommonMistakes), StringComparer.Ordinal)
            .Where(group => group.Count() > MaximumSharingExercises)
            .Select(group => $"« {Truncate(group.Key)} » partagé par "
                + string.Join(", ", group.Select(exercise => exercise.Id).OrderBy(id => id, StringComparer.Ordinal)))
            .ToArray();

        Assert.True(shared.Length == 0, $"Erreurs fréquentes recopiées :\n{string.Join('\n', shared)}");
    }

    [Fact]
    public void NoHintReproducesALineOfItsOwnSolution()
    {
        var offenders = new List<string>();

        foreach (Exercise exercise in ReadExercises())
        {
            string solutionPath = Path.Combine(ExercisesRoot, exercise.Id, "solution", "Submission.cs");
            if (!File.Exists(solutionPath))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(solutionPath)
                .Select(Normalize)
                .Where(line => line.Length >= MinimumMeaningfulSolutionLineLength)
                .ToArray();

            foreach (string hint in exercise.Hints)
            {
                string normalizedHint = Normalize(hint);
                string? leaked = Array.Find(
                    lines,
                    line => normalizedHint.Contains(line, StringComparison.Ordinal));

                if (leaked is not null)
                {
                    offenders.Add($"{exercise.Id} : un indice recopie « {Truncate(leaked)} ».");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    private static IEnumerable<Exercise> ReadExercises()
    {
        foreach (string directory in Directory.GetDirectories(ExercisesRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            string manifestPath = Path.Combine(directory, "exercise.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            JsonElement root = manifest.RootElement;

            yield return new Exercise(
                Path.GetFileName(directory),
                root.GetProperty("hints").EnumerateArray()
                    .Select(hint => hint.GetProperty("content").GetString()!).ToList(),
                root.GetProperty("commonMistakes").EnumerateArray()
                    .Select(mistake => mistake.GetString()!).ToList());
        }
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Truncate(string value) =>
        value.Length <= 90 ? value : value[..90] + "…";

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

    private sealed record Exercise(string Id, List<string> Hints, List<string> CommonMistakes);
}
