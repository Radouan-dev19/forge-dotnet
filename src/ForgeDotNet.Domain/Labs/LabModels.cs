namespace ForgeDotNet.Domain.Labs;

/// <summary>
/// Objectif d'un laboratoire, avec la preuve observable qui permet de dire qu'il est atteint.
/// </summary>
/// <remarks>
/// La preuve est décrite, pas mesurée. C'est la différence entre un laboratoire et un exercice : le
/// second est corrigé par le bac à sable, le premier est vérifié par l'apprenant sur son poste.
/// </remarks>
public sealed record LabObjective(string Id, string Goal, string ObservableProof);

/// <summary>
/// Commande que l'apprenant exécute lui-même, avec ce qu'elle établit.
/// </summary>
public sealed record LabCommand(string Shell, string Command, string Purpose);

/// <summary>
/// Laboratoire publié : un vrai projet exécutable, montré à l'apprenant mais jamais exécuté par
/// Forge.NET.
/// </summary>
/// <remarks>
/// <para>
/// Les six laboratoires sont les seuls endroits du dépôt qui portent un fichier de projet compilable,
/// un conteneur durci, une définition d'infrastructure et une chaîne de livraison. Ils étaient
/// pourtant invisibles du produit : aucun chemin de l'application ne les citait, si bien qu'un
/// apprenant suivant le parcours ne les rencontrait jamais. C'est le même défaut que celui corrigé
/// pour les scénarios SQL, une zone de contenu plus loin.
/// </para>
/// <para>
/// <see cref="EvidencePolicy"/> est la propriété la plus importante de ce type et sa valeur est
/// imposée par le schéma : la réussite d'un laboratoire est <b>déclarée</b> par l'apprenant, hors du
/// bac à sable, et ne produit aucune preuve de maîtrise. Aucun manifeste ne peut prétendre le
/// contraire sans modifier le schéma, ce qui reste une décision visible en revue.
/// </para>
/// </remarks>
public sealed record Lab(
    string Id,
    int Version,
    string Title,
    IReadOnlyList<int> Weeks,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Prerequisites,
    int EstimatedMinutes,
    string Brief,
    IReadOnlyList<LabObjective> Objectives,
    IReadOnlyList<LabCommand> Commands,
    IReadOnlyList<string> Limits,
    string EvidencePolicy,
    bool RequiresDocker,
    bool RequiresNetwork,
    string Revision)
{
    /// <summary>
    /// Seule politique de preuve qu'un laboratoire puisse déclarer dans la version 1 du schéma.
    /// </summary>
    public const string LearnerDeclaredPolicy = "learner-declared-outside-sandbox";

    /// <summary>
    /// Vrai lorsque la réussite du laboratoire est déclarée par l'apprenant et ne prouve rien au
    /// serveur.
    /// </summary>
    /// <remarks>
    /// La propriété existe pour que l'interface n'ait pas à comparer une chaîne, et pour qu'un jour où
    /// un laboratoire deviendrait vérifiable — une suite exécutée par le produit lui-même — le point
    /// de décision soit déjà nommé, à un seul endroit.
    /// </remarks>
    public bool IsLearnerDeclared =>
        string.Equals(EvidencePolicy, LearnerDeclaredPolicy, StringComparison.Ordinal);
}
