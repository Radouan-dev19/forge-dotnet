namespace ForgeDotNet.Domain.Prep;

/// <summary>
/// Dossier de préparation d'entretien publié : un document Markdown autonome, servi tel quel,
/// hors parcours, rattaché à un sous-thème (une entreprise ou un poste visé).
/// </summary>
/// <remarks>
/// <see cref="Order"/> ordonne les dossiers à l'intérieur de leur <see cref="Theme"/> ;
/// <see cref="Body"/> est le texte intégral du Markdown référencé par le manifeste, qui relie les
/// cours de la plateforme plutôt que de les recopier.
/// </remarks>
public sealed record PrepGuide(
    string Id,
    int Version,
    string Title,
    string Summary,
    string Theme,
    string ThemeTitle,
    int Order,
    string Body);
