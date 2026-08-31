using System.Text.RegularExpressions;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Labs;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Curriculum;

/// <summary>
/// Résout un identifiant cité dans une leçon vers la page qui sert l'activité, d'après le
/// catalogue publié et, pour les familles qui vivent dans leur propre catalogue, leurs sources :
/// scénarios SQL (SqlLab) et laboratoires.
/// </summary>
/// <remarks>
/// Seul un identifiant réellement publié devient un lien : un mot de code ordinaire
/// (<c>int</c>, <c>List&lt;T&gt;</c>) reste du code. Les activités s'ouvrent dans un nouvel onglet
/// pour que l'apprenant revienne finir la leçon ; une autre leçon reste une navigation du parcours
/// et s'ouvre dans le même onglet — sur la page du parcours qui la sert : <c>/learn</c> pour le
/// socle, <c>/learn-senior</c> pour la piste senior, dont les identifiants portent le préfixe
/// <c>senior-</c> (convention du dépôt, figée par les relevés de couverture). Un laboratoire est
/// souvent cité par le chemin d'un de ses fichiers, <c>content/labs/&lt;labo&gt;/…</c> : le préfixe
/// suffit à retrouver sa page.
/// </remarks>
public sealed partial class CatalogLessonActivityLinkResolver(
    ContentCatalogProvider catalogProvider,
    ISqlScenarioSource? sqlScenarios = null,
    ILabSource? labs = null) : ILessonActivityLinkResolver
{
    private const string SeniorLessonPrefix = "senior-";

    public async ValueTask<LessonActivityLink?> ResolveAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        Match labPath = LabPathRegex().Match(identifier);
        if (labPath.Success)
        {
            return await ResolveLabAsync(labPath.Groups["lab"].Value, cancellationToken);
        }

        if (!IdentifierRegex().IsMatch(identifier))
        {
            return null;
        }

        ContentCatalogItem? item = catalogProvider.Current.FindById(identifier);
        if (item is not null)
        {
            return item.Type switch
            {
                ContentDocumentType.Exercise => new LessonActivityLink($"/practice/{identifier}", true),
                ContentDocumentType.DebugScenario => new LessonActivityLink($"/debug-lab/{identifier}", true),
                ContentDocumentType.Project => new LessonActivityLink($"/projects/{identifier}", true),
                ContentDocumentType.Lab => new LessonActivityLink($"/labs/{identifier}", true),
                ContentDocumentType.Lesson => new LessonActivityLink(
                    identifier.StartsWith(SeniorLessonPrefix, StringComparison.Ordinal)
                        ? $"/learn-senior/{identifier}"
                        : $"/learn/{identifier}",
                    false),
                _ => null,
            };
        }

        // Les scénarios SQL vivent dans un catalogue distinct (content/sql) : la source du SqlLab
        // est la seule à savoir s'ils sont publiés et exécutables.
        if (sqlScenarios is not null && await sqlScenarios.GetAsync(identifier, cancellationToken) is not null)
        {
            return new LessonActivityLink($"/sql-lab?scenario={identifier}", true);
        }

        return await ResolveLabAsync(identifier, cancellationToken);
    }

    private async ValueTask<LessonActivityLink?> ResolveLabAsync(string labId, CancellationToken cancellationToken)
    {
        // Les laboratoires vivent aussi dans leur propre catalogue (content/labs).
        if (labs is not null && await labs.GetAsync(labId, cancellationToken) is not null)
        {
            return new LessonActivityLink($"/labs/{labId}", true);
        }

        return null;
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*(?:-\d{3})?$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^content/labs/(?<lab>[a-z0-9]+(?:-[a-z0-9]+)*)(?:/.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex LabPathRegex();
}
