namespace ForgeDotNet.Domain.Mastery;

/// <summary>
/// Domaine de maîtrise alimenté par une compétence publiée.
/// </summary>
/// <remarks>
/// La pratique et les examens attribuaient leurs observations à <see cref="MasteryDomain.CSharp"/>
/// par un littéral, quelle que soit la compétence travaillée : résoudre un exercice « api-* »
/// augmentait le score C# et jamais le score Api, qui ne pouvait donc jamais atteindre son seuil.
/// Cette table est la seule source de vérité de cette correspondance ; la dupliquer, c'est risquer
/// qu'une carte de révision et une observation de pratique classent le même exercice ailleurs.
///
/// Un préfixe inconnu lève plutôt que de retomber sur C# : l'auteur qui publie une famille de
/// compétences nouvelle doit dire quel domaine elle alimente, faute de quoi son travail
/// disparaîtrait dans un score qui n'est pas le sien.
/// </remarks>
public static class MasterySkillDomains
{
    private static readonly Dictionary<string, MasteryDomain> ByPrefix = new(StringComparer.Ordinal)
    {
        // Le domaine C# couvre le langage et ce qu'on écrit avec : structures, algorithmes, logique.
        ["csharp"] = MasteryDomain.CSharp,
        ["algorithm"] = MasteryDomain.CSharp,
        ["structures"] = MasteryDomain.CSharp,
        ["logic"] = MasteryDomain.CSharp,

        ["debugging"] = MasteryDomain.Debugging,

        // Les exercices EF Core des semaines SQL pratiquent le relationnel à travers le traducteur
        // de requêtes : leur travail alimente le domaine SQL, aux côtés des scénarios de laboratoire.
        ["sql"] = MasteryDomain.Sql,

        ["api"] = MasteryDomain.Api,
        ["tests"] = MasteryDomain.Tests,
        ["security"] = MasteryDomain.Security,
        ["docker"] = MasteryDomain.Docker,

        // La chaîne de livraison : l'historique Git en fait partie autant que le pipeline.
        ["ci"] = MasteryDomain.ContinuousIntegration,
        ["git"] = MasteryDomain.ContinuousIntegration,

        // Les décisions de structure : revue, qualité, hébergement, conduite de projet.
        ["quality"] = MasteryDomain.Architecture,
        ["azure"] = MasteryDomain.Architecture,
        ["project"] = MasteryDomain.Architecture,

        // Mesurer avant de conclure : latence, alertes, coûts.
        ["observability"] = MasteryDomain.Performance,

        ["english"] = MasteryDomain.English,
    };

    /// <summary>
    /// Domaine alimenté par la première compétence déclarée par un contenu.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Le contenu ne déclare aucune compétence, ou son préfixe n'est rattaché à aucun domaine.
    /// </exception>
    public static MasteryDomain FromSkills(IReadOnlyList<string> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);
        if (skills.Count == 0)
        {
            throw new InvalidOperationException(
                "Un contenu sans compétence déclarée ne peut alimenter aucun domaine de maîtrise.");
        }

        return FromSkill(skills[0]);
    }

    /// <summary>
    /// Domaine alimenté par une compétence, désigné par le préfixe qui précède son premier point.
    /// </summary>
    public static MasteryDomain FromSkill(string skill)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skill);
        int separator = skill.IndexOf('.', StringComparison.Ordinal);
        string prefix = separator < 0 ? skill : skill[..separator];
        return ByPrefix.TryGetValue(prefix, out MasteryDomain domain)
            ? domain
            : throw new InvalidOperationException(
                $"La famille de compétences « {prefix} » n'est rattachée à aucun domaine de maîtrise.");
    }

    /// <summary>Préfixes publiés, exposés pour que les tests de contenu vérifient la couverture.</summary>
    public static IReadOnlyCollection<string> KnownPrefixes => ByPrefix.Keys;
}
