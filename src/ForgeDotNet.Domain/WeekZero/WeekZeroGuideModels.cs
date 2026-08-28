namespace ForgeDotNet.Domain.WeekZero;

/// <summary>
/// Guide de la Semaine 0 publié : un document Markdown autonome, servi tel quel, hors parcours.
/// </summary>
/// <remarks>
/// <see cref="Order"/> n'est qu'une suggestion de lecture — le guide s'aborde selon les besoins,
/// sans prérequis ni progression imposée. <see cref="Body"/> est le texte intégral du Markdown
/// référencé par le manifeste.
/// </remarks>
public sealed record WeekZeroGuide(
    string Id,
    int Version,
    string Title,
    string Summary,
    int Order,
    string Body);
