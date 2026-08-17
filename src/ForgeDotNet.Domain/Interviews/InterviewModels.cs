namespace ForgeDotNet.Domain.Interviews;

/// <summary>Niveau auquel une question d'entretien est posée.</summary>
public enum InterviewLevel
{
    Junior,
    Intermediate,
    Advanced,
}

/// <summary>
/// Fiche d'entretien publiée : une question, ce qu'un examinateur y observe, et une réponse modèle.
/// </summary>
/// <remarks>
/// <para>
/// Ces 242 fiches existaient, validées et comptées, sans aucun écran pour les afficher : le produit
/// les chargeait et personne ne pouvait les lire. Elles ne produisent aucune preuve de maîtrise et
/// n'en produiront pas — un entretien se juge par un interlocuteur qui relance, ce que
/// <c>docs/HUMAN_REVIEW.md</c> confie à un relecteur humain. Leur rôle est la préparation à cette
/// revue, pas son remplacement.
/// </para>
/// <para>
/// <see cref="ObservableCriteria"/> et <see cref="ModelAnswer"/> sont la matière de la grille : le
/// premier dit ce que le candidat doit énoncer de lui-même, le second donne un exemple de contenu
/// suffisant — jamais une formulation à retrouver mot pour mot.
/// </para>
/// </remarks>
public sealed record InterviewSheet(
    string Id,
    int Version,
    string Title,
    InterviewLevel Level,
    int DurationMinutes,
    IReadOnlyList<string> Skills,
    string Question,
    IReadOnlyList<string> ObservableCriteria,
    string ModelAnswer,
    IReadOnlyList<string> CommonMistakes,
    IReadOnlyList<string> Variants)
{
    /// <summary>
    /// Exercice dont cette fiche prolonge le travail, quand la convention de nommage l'établit.
    /// </summary>
    /// <remarks>
    /// 175 des 242 fiches sont nommées <c>interview-&lt;identifiant d'exercice&gt;</c> : le lien est donc
    /// dérivable sans champ supplémentaire dans le manifeste. Les 67 autres portent des décisions qui
    /// n'appartiennent à aucun exercice — elles ne sont atteignables que par l'index, ce qui justifie
    /// l'index à lui seul. Rendre ce lien nul plutôt que de forcer une correspondance évite d'inventer
    /// un rattachement que le contenu ne déclare pas.
    /// </remarks>
    public string? RelatedExerciseId =>
        Id.StartsWith("interview-", StringComparison.Ordinal) && Id.Length > "interview-".Length
            ? Id["interview-".Length..]
            : null;
}

public static class InterviewLevels
{
    public static InterviewLevel Parse(string? value) => value switch
    {
        "junior" => InterviewLevel.Junior,
        "intermediate" => InterviewLevel.Intermediate,
        "advanced" => InterviewLevel.Advanced,
        _ => throw new InvalidDataException($"Niveau d'entretien inconnu : {value}."),
    };

    public static string Label(InterviewLevel level) => level switch
    {
        InterviewLevel.Junior => "Junior",
        InterviewLevel.Intermediate => "Intermédiaire",
        InterviewLevel.Advanced => "Avancé",
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };
}
