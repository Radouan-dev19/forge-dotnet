using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Reviews;

/// <summary>
/// Carte de révision à choix attachée à un exercice, corrigée côté serveur.
/// </summary>
/// <param name="Id">Identifiant déclaré par <c>reviewCards</c> dans le manifeste de l'exercice.</param>
/// <param name="ExerciseId">Exercice d'origine, qui conditionne l'apparition de la carte.</param>
/// <param name="Domain">Domaine de maîtrise alimenté par cette carte.</param>
/// <param name="Question">Énoncé présenté à l'apprenant.</param>
/// <param name="CorrectOptionId">Option attendue. Elle ne quitte jamais le serveur.</param>
/// <param name="Options">Options proposées, dans l'ordre de publication.</param>
public sealed record ExerciseReviewCard(
    string Id,
    string ExerciseId,
    MasteryDomain Domain,
    string Question,
    string CorrectOptionId,
    IReadOnlyList<ExerciseReviewOption> Options);

/// <param name="Id">Identifiant court de l'option, envoyé comme réponse.</param>
/// <param name="Text">Libellé affiché.</param>
public sealed record ExerciseReviewOption(string Id, string Text);

/// <summary>
/// Fournit les cartes de révision du catalogue.
/// </summary>
/// <remarks>
/// Ces cartes sont la seule source de rétention espacée qui vive à l'échelle du parcours : le bilan
/// d'entrée n'est passé qu'une fois et ses preuves expirent, ce qui laissait la composante sans
/// alimentation possible au-delà du délai de validité.
/// </remarks>
public interface IReviewCardSource
{
    /// <summary>Cartes déclarées pour l'exercice, ou une liste vide s'il n'en porte aucune.</summary>
    ValueTask<IReadOnlyList<ExerciseReviewCard>> GetForExerciseAsync(
        string exerciseId,
        CancellationToken cancellationToken = default);
}
