namespace ForgeDotNet.Domain.Ai;

/// <summary>
/// Guide du chapitre IA publié : un document Markdown autonome, servi tel quel, hors parcours.
/// </summary>
/// <remarks>
/// <see cref="Order"/> n'est qu'une suggestion de lecture — le chapitre s'aborde selon les besoins,
/// sans prérequis ni progression imposée. <see cref="Body"/> est le texte intégral du Markdown
/// référencé par le manifeste.
/// </remarks>
public sealed record AiGuide(
    string Id,
    int Version,
    string Title,
    string Summary,
    int Order,
    string Body);
