using System.Text.RegularExpressions;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Fige la lisibilité des artefacts que l'apprenant paie le plus cher à consulter.
/// </summary>
/// <remarks>
/// <para>
/// Ces deux règles couvrent un angle mort mesuré : ni les schémas, ni les règles d'authenticité,
/// ni <c>dotnet format</c> ne voient ces fichiers, car ils sont du contenu et non du code compilé
/// par le produit. Au relevé initial, 84 solutions sur 148 portaient au moins une ligne dépassant
/// 120 caractères — record à 414 — et 136 explications sur 148 restaient sous 350 mots, avec une
/// médiane à 115.
/// </para>
/// <para>
/// Pourquoi ces deux seuils. Ouvrir la solution met la pratique de l'exercice à zéro,
/// définitivement : l'apprenant paie le prix maximal pour lire cet artefact, qui doit donc être
/// exemplaire — une instruction par ligne, dans une largeur qu'aucune revue ne refuserait.
/// L'explication est la boucle de retour juste après l'effort, sur une pratique qui pèse 45 % du
/// score : cent mots de paraphrase n'y enseignent rien, d'où un plancher aligné sur l'étalon
/// <c>api-validation-aggregate-001</c>.
/// </para>
/// <para>
/// Les deux plafonds fonctionnent comme le registre de dette éditoriale : ils sont à zéro et ne
/// peuvent que descendre — les remonter est une décision humaine visible en revue, jamais un
/// moyen de faire passer un contenu neuf.
/// </para>
/// </remarks>
[Trait("Category", "ExerciseReadability")]
public sealed class ExerciseReadabilityTests
{
    /// <summary>Largeur maximale d'une ligne de starter ou de solution publiés.</summary>
    private const int MaximumLineLength = 120;

    /// <summary>Nombre minimal de mots d'une explication d'après-exercice.</summary>
    private const int MinimumExplanationWords = 350;

    /// <summary>Plafond d'exercices en infraction sur la largeur. Il ne peut que descendre.</summary>
    private const int MaximumOverlongOffenders = 0;

    /// <summary>Plafond d'explications trop courtes. Il ne peut que descendre.</summary>
    private const int MaximumThinExplanations = 0;

    private static readonly string ExercisesRoot = Path.Combine(
        FindRepositoryRoot(), "content", "reference", "exercises");

    [Fact]
    public void NoPublishedSubmissionFileCarriesAnOverlongLine()
    {
        var offenders = new List<string>();

        foreach (string directory in Directory.GetDirectories(ExercisesRoot).OrderBy(p => p, StringComparer.Ordinal))
        {
            foreach (string relative in new[] { "solution/Submission.cs", "starter/Submission.cs" })
            {
                string path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    continue;
                }

                int worst = File.ReadAllLines(path)
                    .Select(line => line.TrimEnd().Length)
                    .DefaultIfEmpty(0)
                    .Max();
                if (worst > MaximumLineLength)
                {
                    offenders.Add($"{Path.GetFileName(directory)}/{relative} : {worst} caractères.");
                }
            }
        }

        Assert.True(
            offenders.Count <= MaximumOverlongOffenders,
            $"{offenders.Count} fichier(s) au-delà de {MaximumLineLength} caractères pour un plafond de "
            + $"{MaximumOverlongOffenders} :" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void EveryExplanationCarriesEnoughSubstanceToTeach()
    {
        var offenders = new List<string>();

        foreach (string directory in Directory.GetDirectories(ExercisesRoot).OrderBy(p => p, StringComparer.Ordinal))
        {
            string path = Path.Combine(directory, "explanation.md");
            if (!File.Exists(path))
            {
                continue;
            }

            // Le compte s'appuie sur la séparation par blancs : simple, stable, et suffisant pour
            // distinguer une paraphrase de cent mots d'une explication qui argumente ses décisions.
            int words = Regex.Matches(File.ReadAllText(path), @"\S+").Count;
            if (words < MinimumExplanationWords)
            {
                offenders.Add($"{Path.GetFileName(directory)} : {words} mot(s).");
            }
        }

        Assert.True(
            offenders.Count <= MaximumThinExplanations,
            $"{offenders.Count} explication(s) sous {MinimumExplanationWords} mots pour un plafond de "
            + $"{MaximumThinExplanations} :" + Environment.NewLine + string.Join(Environment.NewLine, offenders.Take(20)));
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
