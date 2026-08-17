using System.Text.Json;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.UnitTests;

/// <summary>
/// Calcule, domaine par domaine, le score maximal que les producteurs d'observations rendent
/// atteignable, et refuse qu'un domaine plafonne à son propre seuil ou en dessous.
/// </summary>
/// <remarks>
/// Les règles de maîtrise sont justes et prouvées ; ce sont les producteurs qui manquent. Le poids
/// d'une composante sans preuve n'étant jamais redistribué, deux composantes sans producteur —
/// explication et quiz — plafonnent tout domaine à 85, soit exactement le seuil critique. Pire, la
/// pratique et les examens attribuent leurs observations à un domaine codé en dur : un exercice
/// « api-* » alimente le score C# et jamais le score Api, qui plafonne donc à 15 pour un seuil de 85.
///
/// Aucune de ces impossibilités n'était visible. Le tableau affichait un score qui ne pouvait pas
/// atteindre sa propre barre, et l'apprenant en concluait qu'il n'était pas au niveau. Ce test rend
/// le plafond calculable : il ne juge pas d'un profil, il juge du produit.
/// </remarks>
public sealed class MasteryReachabilityTests
{
    /// <summary>
    /// Domaines qu'un producteur d'observations alimente réellement, composante par composante.
    /// </summary>
    /// <remarks>
    /// Cet inventaire est tenu à la main et décrit <c>SqliteMasteryEvidenceSource</c> et
    /// <c>FileSystemExamBankSource</c> tels qu'ils sont, non tels qu'on les voudrait. Toute
    /// correction d'attribution doit s'y refléter, sinon le calcul ment.
    ///
    /// La rétention est déclarée pour les seuls domaines couverts par la banque de cartes. Une
    /// question ratée du bilan d'entrée en offre une seconde voie, mais elle exige d'avoir échoué et
    /// ses preuves expirent : la compter reviendrait à surestimer le plafond.
    /// </remarks>
    private static readonly Dictionary<MasteryComponent, MasteryDomain[]> ProducedBy = new()
    {
        // AddPracticeAsync déduit le domaine de la compétence de l'exercice ; AddDebugAsync et
        // AddSqlAsync ajoutent le débogage et SQL, dont la pratique passe par des scénarios.
        [MasteryComponent.AutonomousPractice] = AllDomains,

        // FileSystemExamBankSource déduit le domaine de l'item tiré, exercice ou scénario SQL.
        [MasteryComponent.UnassistedExam] = AllDomains,

        // La banque couvre les exercices publiés et les 40 scénarios SQL ; ReviewCardQualityTests
        // en tient le plancher, et exige la couverture intégrale des domaines critiques.
        [MasteryComponent.SpacedRetention] = AllDomains,

        // AddQuizAsync : le domaine vient de la première compétence de la leçon, et les 70 leçons
        // couvrent l'ensemble des domaines du parcours.
        [MasteryComponent.Quiz] = AllDomains,
    };

    /// <summary>
    /// Composantes qu'aucun producteur n'alimente. Ce nombre ne peut que descendre.
    /// </summary>
    /// <remarks>
    /// L'explication y restera, et ce n'est pas une dette : trois routes ont été cherchées puis
    /// refusées — la carte à choix sur le « pourquoi » mesure la reconnaissance, l'explication
    /// personnelle du protocole de pratique mesure l'effort et n'est atteignable qu'après une solution
    /// consultée, la rubrique déterministe note une transcription ou une devinette selon qu'elle publie
    /// ou non ses concepts obligatoires. Le raisonnement complet est dans <c>docs/MASTERY.md</c> ;
    /// <c>MasteryRulesTests</c> le rend exécutable en refusant les moteurs qui prétendraient l'alimenter
    /// et en montrant que le plafond de 90 qui en résulte franchit les deux seuils.
    /// </remarks>
    private static readonly MasteryComponent[] UnproducedComponents = [MasteryComponent.Explanation];

    private const int MaximumUnproducedComponents = 1;

    /// <summary>
    /// Les onze domaines, tous couverts par au moins un exercice ou un scénario publié.
    /// </summary>
    private static MasteryDomain[] AllDomains => Enum.GetValues<MasteryDomain>();

    [Fact]
    public void EveryDomainCanReachItsOwnThreshold()
    {
        MasteryPolicy policy = MasteryPolicyCatalog.Version1;
        var unreachable = new List<string>();

        foreach (MasteryDomain domain in Enum.GetValues<MasteryDomain>())
        {
            decimal ceiling = Ceiling(policy, domain);
            decimal threshold = Threshold(policy, domain);
            if (ceiling < threshold)
            {
                unreachable.Add(
                    $"{domain} : plafond {ceiling:0.##} pour un seuil de {threshold:0.##} — inatteignable.");
            }
            else if (ceiling == threshold)
            {
                // Un plafond égal au seuil n'est franchi qu'avec cent sur chaque composante produite,
                // sans le moindre indice : c'est un blocage déguisé en objectif.
                unreachable.Add(
                    $"{domain} : plafond {ceiling:0.##} égal au seuil — atteignable uniquement à 100 partout.");
            }
        }

        Assert.True(
            unreachable.Count == 0,
            $"{unreachable.Count} domaine(s) ne peuvent pas atteindre leur seuil par le travail :"
            + Environment.NewLine + string.Join(Environment.NewLine, unreachable));
    }

    /// <summary>
    /// Chaque score de domaine exigé par une porte doit rester atteignable.
    /// </summary>
    [Fact]
    public void EveryDomainScoreRequiredByAGateIsReachable()
    {
        MasteryPolicy policy = MasteryPolicyCatalog.Version1;
        var unreachable = new List<string>();

        foreach (MasteryGatePolicy gate in policy.Gates)
        {
            foreach (MasteryGateRequirement requirement in gate.Requirements
                .Where(item => item.Kind == MasteryGateRequirementKind.DomainScore))
            {
                decimal ceiling = Ceiling(policy, requirement.Domain!.Value);
                if (ceiling <= requirement.MinimumScore)
                {
                    unreachable.Add(
                        $"Porte {gate.Gate} — « {requirement.Label} » : plafond {ceiling:0.##}.");
                }
            }
        }

        Assert.True(
            unreachable.Count == 0,
            "Des conditions de porte sont hors d'atteinte :" + Environment.NewLine
            + string.Join(Environment.NewLine, unreachable));
    }

    /// <summary>
    /// Un domaine dont le score est atteignable doit aussi porter assez d'éléments distincts pour
    /// être validé.
    /// </summary>
    /// <remarks>
    /// Le plafond de score et la validation d'un domaine sont deux conditions différentes, et rien
    /// ne le disait. La politique exige <c>MinimumDistinctItems</c> exercices autonomes distincts
    /// pour valider un domaine : un domaine qui n'en publie que deux atteint quatre-vingt-dix en
    /// score et reste invalidable à jamais, sans qu'aucun test ne s'en aperçoive.
    ///
    /// Les éléments sont comptés comme les producteurs les attribuent — la compétence de l'exercice
    /// via <see cref="MasterySkillDomains"/>, le débogage par ses scénarios, SQL par les siens —
    /// faute de quoi le compte décrirait un produit imaginaire.
    /// </remarks>
    [Fact]
    public void EveryReachableDomainHasEnoughDistinctItemsToBeValidated()
    {
        MasteryPolicy policy = MasteryPolicyCatalog.Version1;
        Dictionary<MasteryDomain, int> items = CountItemsByDomain();
        var starved = new List<string>();

        foreach (MasteryDomain domain in Enum.GetValues<MasteryDomain>())
        {
            if (Ceiling(policy, domain) <= 0m)
            {
                continue;
            }

            int count = items.GetValueOrDefault(domain);
            if (count < policy.MinimumDistinctItems)
            {
                starved.Add(
                    $"{domain} : {count} élément(s) pratiquable(s) pour {policy.MinimumDistinctItems} exigés.");
            }
        }

        Assert.True(
            starved.Count == 0,
            $"{starved.Count} domaine(s) ne peuvent pas être validés faute d'éléments distincts :"
            + Environment.NewLine + string.Join(Environment.NewLine, starved));
    }

    /// <summary>
    /// Éléments pratiquables par domaine, lus dans le catalogue publié.
    /// </summary>
    private static Dictionary<MasteryDomain, int> CountItemsByDomain()
    {
        string root = FindRepositoryRoot();
        var counts = new Dictionary<MasteryDomain, int>();

        foreach (string manifest in Directory.EnumerateFiles(
            Path.Combine(root, "content", "reference", "exercises"), "exercise.json", SearchOption.AllDirectories))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest));
            MasteryDomain domain = MasterySkillDomains.FromSkill(
                document.RootElement.GetProperty("skills")[0].GetString()!);
            counts[domain] = counts.GetValueOrDefault(domain) + 1;
        }

        // La pratique du débogage et celle de SQL passent par des scénarios, que AddDebugAsync et
        // AddSqlAsync attribuent directement à leur domaine.
        counts[MasteryDomain.Debugging] = counts.GetValueOrDefault(MasteryDomain.Debugging)
            + Directory.GetDirectories(Path.Combine(root, "content", "reference", "debugging")).Length;
        counts[MasteryDomain.Sql] = counts.GetValueOrDefault(MasteryDomain.Sql)
            + Directory.GetFiles(Path.Combine(root, "content", "sql"), "scenario.json", SearchOption.AllDirectories).Length;

        return counts;
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

    [Fact]
    public void EveryComponentIsEitherProducedOrDeclaredUnproduced()
    {
        MasteryComponent[] declared = MasteryPolicyCatalog.Version1.Components
            .Select(component => component.Component)
            .ToArray();
        MasteryComponent[] inventoried = [.. ProducedBy.Keys, .. UnproducedComponents];

        Assert.Empty(declared.Except(inventoried));
        Assert.Empty(inventoried.Except(declared));
        Assert.Equal(inventoried.Length, inventoried.Distinct().Count());
    }

    [Fact]
    public void TheNumberOfComponentsWithoutAProducerNeverGrows()
    {
        Assert.True(
            UnproducedComponents.Length <= MaximumUnproducedComponents,
            $"{UnproducedComponents.Length} composantes sans producteur, pour un plafond de "
            + $"{MaximumUnproducedComponents}.");
    }

    private static decimal Threshold(MasteryPolicy policy, MasteryDomain domain) =>
        policy.CriticalDomains.Contains(domain)
            ? policy.CriticalModuleThreshold
            : policy.ModuleThreshold;

    /// <summary>
    /// Score maximal d'un domaine : la somme des poids des composantes qu'un producteur alimente,
    /// chacune portée à cent.
    /// </summary>
    private static decimal Ceiling(MasteryPolicy policy, MasteryDomain domain) => policy.Components
        .Where(component => ProducedBy.TryGetValue(component.Component, out MasteryDomain[]? domains)
            && domains.Contains(domain))
        .Sum(component => component.Weight * 100m);
}
