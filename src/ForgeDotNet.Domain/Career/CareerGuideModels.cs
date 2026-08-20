namespace ForgeDotNet.Domain.Career;

/// <summary>
/// Guide de carrière publié : un document Markdown autonome, servi tel quel.
/// </summary>
/// <remarks>
/// <see cref="Order"/> porte l'ordre pédagogique de lecture — le CV avant la prospection, la
/// négociation avant la prise de poste — que l'ordre alphabétique des identifiants ne donne pas.
/// <see cref="Body"/> est le texte intégral du Markdown référencé par le manifeste : un guide se lit
/// en une page, sans navigation interne ni ressource externe obligatoire.
/// </remarks>
public sealed record CareerGuide(
    string Id,
    int Version,
    string Title,
    string Summary,
    int Order,
    string Body);
