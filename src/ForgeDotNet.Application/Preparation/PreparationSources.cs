using ForgeDotNet.Domain.Ai;
using ForgeDotNet.Domain.Career;
using ForgeDotNet.Domain.English;
using ForgeDotNet.Domain.Interviews;

namespace ForgeDotNet.Application.Preparation;

/// <summary>
/// Sources des contenus de préparation : ils s'étudient, ils ne se notent pas.
/// </summary>
/// <remarks>
/// Fiches d'entretien et cartes d'anglais partagent une propriété qui justifie de les regrouper :
/// aucune ne produit d'observation de maîtrise, et aucune ne le pourra. Ce qu'elles préparent est une
/// revue par un tiers, décrite dans <c>docs/HUMAN_REVIEW.md</c>. Les placer dans un espace nommé
/// distinct de <c>Practice</c> évite qu'un incrément futur les branche par inadvertance sur un
/// producteur, en rendant visible dans le chemin de type qu'elles n'y appartiennent pas.
/// </remarks>
public interface IInterviewSource
{
    ValueTask<IReadOnlyList<InterviewSheet>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<InterviewSheet?> GetAsync(string interviewId, CancellationToken cancellationToken = default);

    /// <summary>Fiche prolongeant un exercice donné, lorsque la convention de nommage l'établit.</summary>
    ValueTask<InterviewSheet?> FindForExerciseAsync(
        string exerciseId,
        CancellationToken cancellationToken = default);
}

public interface IEnglishActivitySource
{
    ValueTask<IReadOnlyList<EnglishActivity>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<EnglishActivity?> GetAsync(string activityId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Guides de carrière de la semaine 24 : ils se lisent et s'appliquent hors du produit.
/// </summary>
/// <remarks>
/// Même famille de préparation que les fiches et les cartes : aucun guide ne produit d'observation
/// de maîtrise — un CV, une candidature ou une négociation se jugent hors de toute instrumentation.
/// </remarks>
public interface ICareerGuideSource
{
    ValueTask<IReadOnlyList<CareerGuide>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<CareerGuide?> GetAsync(string guideId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Guides du chapitre IA : un outillage de métier à lire selon les besoins, hors parcours.
/// </summary>
/// <remarks>
/// Même famille que les autres contenus de préparation : le runner n'ayant pas de réseau par
/// conception, aucun usage d'assistant n'est vérifiable — ces guides ne produisent donc aucune
/// observation de maîtrise, et ne le pourront pas.
/// </remarks>
public interface IAiGuideSource
{
    ValueTask<IReadOnlyList<AiGuide>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<AiGuide?> GetAsync(string guideId, CancellationToken cancellationToken = default);
}
