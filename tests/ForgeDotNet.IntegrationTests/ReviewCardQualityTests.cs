using System.Text.Json;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Garde-fou sur la banque de cartes de révision, seule source de rétention espacée qui vive à
/// l'échelle du parcours.
/// </summary>
/// <remarks>
/// La composante de rétention ne pouvait être alimentée que par une question ratée du bilan
/// d'entrée : trente-six questions, passées une fois, dont les preuves expirent. Son poids n'étant
/// jamais redistribué, le score d'un domaine plafonnait alors sous le seuil critique quel que soit le
/// travail fourni — ce que <c>MasteryRulesTests</c> documente et prouve.
///
/// Les cartes d'exercice comblent ce manque. Encore faut-il qu'elles ne soient pas ce qu'étaient les
/// cartes affichées : deux cent soixante-dix cartes pour cent énoncés distincts, une même question
/// revenant soixante-quinze fois. Une banque dupliquée transformerait la rétention en récitation
/// d'une seule phrase.
/// </remarks>
public sealed class ReviewCardQualityTests
{
    /// <summary>
    /// Exercices couverts par la banque. Ce plancher ne peut que monter : le baisser exige de
    /// modifier ce fichier, ce qui reste visible en revue.
    /// </summary>
    private const int MinimumCoveredExercises = 182;

    /// <summary>
    /// Nom que le contenu donne à chaque domaine de maîtrise, tel que le schéma des cartes l'énumère.
    /// </summary>
    /// <remarks>
    /// La correspondance entre une compétence et son domaine vit dans
    /// <see cref="MasterySkillDomains"/> — la dupliquer ici ferait qu'une carte et une observation de
    /// pratique pourraient classer le même exercice dans deux domaines différents. Seule la traduction
    /// du domaine vers son nom de contenu reste locale, à l'image de ce que fait
    /// <c>CatalogReviewCardSource</c> dans l'autre sens.
    /// </remarks>
    private static readonly Dictionary<MasteryDomain, string> ContentNames = new()
    {
        [MasteryDomain.CSharp] = "csharp",
        [MasteryDomain.Debugging] = "debugging",
        [MasteryDomain.Sql] = "sql",
        [MasteryDomain.Api] = "api",
        [MasteryDomain.Tests] = "tests",
        [MasteryDomain.Docker] = "docker",
        [MasteryDomain.ContinuousIntegration] = "continuous-integration",
        [MasteryDomain.Security] = "security",
        [MasteryDomain.Architecture] = "architecture",
        [MasteryDomain.Performance] = "performance",
        [MasteryDomain.English] = "english",
    };

    /// <summary>
    /// Domaines à seuil critique dont la pratique passe par des exercices.
    /// </summary>
    /// <remarks>
    /// SQL est absent, et cette absence est le fait à retenir : aucun exercice ne porte de
    /// compétence <c>sql.*</c>, sa pratique passant par des scénarios de laboratoire. Sa rétention
    /// demande donc une source distincte, qui n'existe pas encore.
    /// </remarks>
    private static readonly MasteryDomain[] CriticalExerciseDomains =
    [
        MasteryDomain.CSharp,
        MasteryDomain.Debugging,
        MasteryDomain.Api,
        MasteryDomain.Tests,
    ];

    /// <summary>
    /// Part maximale des cartes partageant la même position de bonne réponse. Au-delà, la position
    /// devient un indice qui dispense de connaître la réponse.
    /// </summary>
    private const double MaximumAnswerPositionShare = 0.40;

    /// <summary>
    /// Longueur minimale d'une valeur de cas caché prise en compte : en dessous, une coïncidence
    /// textuelle ne prouve rien.
    /// </summary>
    private const int MinimumRevealingValueLength = 4;

    private static readonly string CatalogRoot = Path.Combine(
        FindRepositoryRoot(), "content", "reference");

    private static readonly string BankPath = Path.Combine(
        CatalogRoot, "reviews", "exercise-review-cards.json");

    [Fact]
    public void EveryCardIsDeclaredByTheExerciseItClaims()
    {
        var offenders = new List<string>();

        foreach (Card card in ReadCards())
        {
            string manifestPath = ManifestPathOf(card);
            if (!File.Exists(manifestPath))
            {
                offenders.Add($"{card.Id} : exercice « {card.ExerciseId} » introuvable.");
                continue;
            }

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            string[] declared = manifest.RootElement.GetProperty("reviewCards").EnumerateArray()
                .Select(item => item.GetString()!)
                .ToArray();

            if (!declared.Contains(card.Id, StringComparer.Ordinal))
            {
                offenders.Add($"{card.Id} : non déclarée par {card.ExerciseId}.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    [Fact]
    public void TheBankCoversAtLeastTheRatchetedNumberOfExercises()
    {
        int covered = ReadCards()
            .Select(card => card.ExerciseId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        Assert.True(
            covered >= MinimumCoveredExercises,
            $"La banque ne couvre plus que {covered} exercices, contre {MinimumCoveredExercises} attendus.");
    }

    /// <summary>
    /// Tout exercice d'un domaine critique porte ses cartes.
    /// </summary>
    /// <remarks>
    /// C'est la condition qui manquait : la mécanique de rétention avait beau accepter les cartes
    /// d'exercice, le domaine C# n'en comptait qu'une sur soixante-quatorze, et la porte A exige
    /// « C# ≥ 85 ». Un domaine critique partiellement couvert laisse l'apprenant devant un score
    /// plafonné sans rien qui le lui explique. Le cliquet global ne suffisait pas : il pouvait monter
    /// en couvrant des domaines qui ne bloquent aucune porte.
    /// </remarks>
    [Fact]
    public void EveryExerciseOfACriticalDomainCarriesItsCards()
    {
        HashSet<string> covered = ReadCards()
            .Select(card => card.ExerciseId)
            .ToHashSet(StringComparer.Ordinal);

        string[] uncovered = Directory
            .EnumerateDirectories(Path.Combine(CatalogRoot, "exercises"))
            .Select(directory => (Id: Path.GetFileName(directory)!, Domain: DomainOf(directory)))
            .Where(exercise => CriticalExerciseDomains.Contains(exercise.Domain))
            .Where(exercise => !covered.Contains(exercise.Id))
            .Select(exercise => $"{exercise.Id} ({ContentNames[exercise.Domain]})")
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            uncovered.Length == 0,
            $"Exercices d'un domaine critique sans carte de révision :\n{string.Join('\n', uncovered)}");
    }

    /// <summary>
    /// Le domaine déclaré par une carte est celui de l'exercice dont elle est issue.
    /// </summary>
    /// <remarks>
    /// Une carte mal classée alimenterait la rétention d'un domaine que l'apprenant n'a pas
    /// travaillé, et priverait de la sienne celui qu'il vient de pratiquer. Un préfixe de compétence
    /// inconnu fait échouer le test plutôt que de passer en silence : l'auteur qui étend la
    /// couverture à une nouvelle famille doit déclarer le domaine qu'elle alimente.
    /// </remarks>
    [Fact]
    public void EveryCardDeclaresTheDomainOfItsExercise()
    {
        var offenders = new List<string>();

        foreach (Card card in ReadCards())
        {
            string expected = card.ItemKind == "sql-scenario"
                ? ContentNames[MasteryDomain.Sql]
                : ContentNames[DomainOf(Path.Combine(CatalogRoot, "exercises", card.ExerciseId))];
            if (!string.Equals(card.Domain, expected, StringComparison.Ordinal))
            {
                offenders.Add($"{card.Id} : déclare « {card.Domain} » là où {card.ExerciseId} relève de « {expected} ».");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    [Fact]
    public void EveryQuestionIsDistinct()
    {
        string[] repeated = ReadCards()
            .GroupBy(card => card.Question, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"« {Truncate(group.Key)} » partagé par "
                + string.Join(", ", group.Select(card => card.Id).OrderBy(id => id, StringComparer.Ordinal)))
            .ToArray();

        Assert.True(repeated.Length == 0, $"Énoncés recopiés :\n{string.Join('\n', repeated)}");
    }

    [Fact]
    public void EveryCardOffersDistinctOptionsAndExactlyOneCorrectAnswer()
    {
        var offenders = new List<string>();

        foreach (Card card in ReadCards())
        {
            if (card.Options.Count < 3)
            {
                offenders.Add($"{card.Id} : {card.Options.Count} option(s), trois au minimum.");
            }

            if (card.Options.Select(option => option.Id).Distinct(StringComparer.Ordinal).Count() != card.Options.Count)
            {
                offenders.Add($"{card.Id} : deux options portent le même identifiant.");
            }

            if (card.Options.Select(option => option.Text).Distinct(StringComparer.Ordinal).Count() != card.Options.Count)
            {
                offenders.Add($"{card.Id} : deux options portent le même libellé.");
            }

            int correct = card.Options.Count(option =>
                string.Equals(option.Id, card.CorrectOptionId, StringComparison.Ordinal));
            if (correct != 1)
            {
                offenders.Add($"{card.Id} : {correct} option(s) correspondent à la réponse attendue.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    [Fact]
    public void TheCorrectAnswerPositionIsNotPredictable()
    {
        Card[] cards = ReadCards().ToArray();
        string[] overused = cards
            .GroupBy(card => card.CorrectOptionId, StringComparer.Ordinal)
            .Where(group => (double)group.Count() / cards.Length > MaximumAnswerPositionShare)
            .Select(group => $"position « {group.Key} » : {group.Count()} cartes sur {cards.Length}")
            .ToArray();

        Assert.True(
            overused.Length == 0,
            $"La position de la bonne réponse devient un indice :\n{string.Join('\n', overused)}");
    }

    /// <summary>
    /// Aucune carte ne doit reproduire un résultat attendu qui n'est connu que des cas cachés.
    /// </summary>
    /// <remarks>
    /// La règle porte sur les seuls résultats attendus, et seulement sur ceux qui n'apparaissent
    /// pas déjà dans l'énoncé, les contraintes ou l'exemple publics de l'exercice. Un argument
    /// caché reprend souvent le vocabulaire du problème — « section », « clé » — et le signaler
    /// serait du bruit ; un résultat attendu inédit, lui, est bien une information réservée.
    /// La comparaison est sensible à la casse : c'est le littéral exact qui constituerait la fuite.
    /// </remarks>
    [Fact]
    public void NoCardRevealsAnExpectedValueKnownOnlyToItsHiddenCases()
    {
        var offenders = new List<string>();

        foreach (Card card in ReadCards())
        {
            if (card.ItemKind != "exercise")
            {
                continue;
            }

            string exerciseDirectory = Path.Combine(CatalogRoot, "exercises", card.ExerciseId);
            string hiddenPath = Path.Combine(exerciseDirectory, "tests", "hidden", "cases.json");
            if (!File.Exists(hiddenPath))
            {
                continue;
            }

            string publicText = PublicText(exerciseDirectory);
            string text = card.Question + " " + string.Join(' ', card.Options.Select(option => option.Text));

            foreach (string value in HiddenExpectedValues(hiddenPath))
            {
                if (publicText.Contains(value, StringComparison.Ordinal))
                {
                    continue;
                }

                if (text.Contains(value, StringComparison.Ordinal))
                {
                    offenders.Add($"{card.Id} : divulgue « {value} », résultat réservé aux cas cachés.");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    private static string PublicText(string exerciseDirectory)
    {
        string statementPath = Path.Combine(exerciseDirectory, "statement.md");
        string statement = File.Exists(statementPath) ? File.ReadAllText(statementPath) : string.Empty;
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(exerciseDirectory, "exercise.json")));

        string constraints = string.Join(
            ' ',
            manifest.RootElement.GetProperty("constraints").EnumerateArray().Select(item => item.GetString()));
        string examples = string.Join(
            ' ',
            manifest.RootElement.GetProperty("examples").EnumerateArray()
                .SelectMany(example => new[]
                {
                    example.GetProperty("input").GetString(),
                    example.GetProperty("output").GetString(),
                }));

        return $"{statement} {constraints} {examples}";
    }

    private static IEnumerable<string> HiddenExpectedValues(string hiddenPath)
    {
        using JsonDocument cases = JsonDocument.Parse(File.ReadAllText(hiddenPath));
        var values = new List<string>();
        foreach (JsonElement testCase in cases.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (testCase.TryGetProperty("expected", out JsonElement expected))
            {
                Collect(expected, values);
            }
        }

        return values.Distinct(StringComparer.Ordinal);

        static void Collect(JsonElement element, List<string> values)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    string value = element.GetString()!;
                    if (value.Length >= MinimumRevealingValueLength)
                    {
                        values.Add(value);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        Collect(item, values);
                    }

                    break;
                default:
                    break;
            }
        }
    }

    private static IEnumerable<Card> ReadCards()
    {
        using JsonDocument bank = JsonDocument.Parse(File.ReadAllText(BankPath));
        var cards = new List<Card>();
        foreach (JsonElement card in bank.RootElement.GetProperty("cards").EnumerateArray())
        {
            cards.Add(new Card(
                card.GetProperty("id").GetString()!,
                card.GetProperty("exerciseId").GetString()!,
                card.TryGetProperty("itemKind", out JsonElement kind) ? kind.GetString()! : "exercise",
                card.GetProperty("domain").GetString()!,
                card.GetProperty("question").GetString()!,
                card.GetProperty("correctOptionId").GetString()!,
                card.GetProperty("options").EnumerateArray()
                    .Select(option => new Option(
                        option.GetProperty("id").GetString()!,
                        option.GetProperty("text").GetString()!))
                    .ToArray()));
        }

        return cards;
    }

    /// <summary>
    /// Manifeste qui déclare les cartes d'un élément : un exercice ou un scénario SQL.
    /// </summary>
    /// <remarks>
    /// La pratique du domaine SQL passe par des scénarios de laboratoire, pas par des exercices.
    /// Sans cette seconde famille, sa composante de rétention restait vide en permanence et son
    /// score plafonnait sous le seuil que la porte A exige.
    /// </remarks>
    private static string ManifestPathOf(Card card) => card.ItemKind == "sql-scenario"
        ? Path.Combine(Path.GetDirectoryName(CatalogRoot)!, "sql", card.ExerciseId, "scenario.json")
        : Path.Combine(CatalogRoot, "exercises", card.ExerciseId, "exercise.json");

    /// <summary>
    /// Domaine d'un exercice, déduit de sa première compétence par la même table que la projection
    /// de maîtrise.
    /// </summary>
    private static MasteryDomain DomainOf(string exerciseDirectory)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(exerciseDirectory, "exercise.json")));
        return MasterySkillDomains.FromSkill(manifest.RootElement.GetProperty("skills")[0].GetString()!);
    }

    private static string Truncate(string value) => value.Length <= 90 ? value : value[..90] + "…";

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

    private sealed record Option(string Id, string Text);

    private sealed record Card(
        string Id,
        string ExerciseId,
        string ItemKind,
        string Domain,
        string Question,
        string CorrectOptionId,
        IReadOnlyList<Option> Options);
}
