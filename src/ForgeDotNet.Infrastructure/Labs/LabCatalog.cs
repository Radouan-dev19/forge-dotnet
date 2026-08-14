using ForgeDotNet.Application.Content;
using ForgeDotNet.Infrastructure.Content;

namespace ForgeDotNet.Infrastructure.Labs;

/// <summary>
/// Snapshot immuable des laboratoires publiés, distinct du catalogue du lecteur.
/// </summary>
/// <remarks>
/// Les laboratoires vivent sous <c>content/labs</c> et non sous <c>content/reference</c> : ils ont
/// donc leur propre catalogue validé, chargé atomiquement au démarrage, exactement comme les scénarios
/// SQL. La raison est la même et elle est structurante : le catalogue du lecteur porte des documents
/// qui se lisent, alors qu'un laboratoire porte une arborescence de code — fichier de projet, image de
/// conteneur, définition d'infrastructure, chaîne de livraison.
///
/// Sans ce chargement, les six laboratoires livrés restaient invisibles du produit, ce qui reproduisait
/// pour eux le défaut relevé sur les scénarios SQL.
/// </remarks>
public sealed class LabCatalog : IDisposable
{
    private readonly ContentCatalogProvider _provider;

    public LabCatalog(ContentValidationOptions validationOptions, string labDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(validationOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(labDirectoryPath);
        LabDirectoryPath = labDirectoryPath;
        _provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
            new FileSystemContentValidationService(validationOptions),
            validationOptions));
    }

    public string LabDirectoryPath { get; }

    public ContentCatalogProvider Provider => _provider;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ContentCatalogReloadResult reload = await _provider.ReloadAsync(LabDirectoryPath, cancellationToken);
        if (!reload.Succeeded)
        {
            throw new InvalidDataException(
                $"Le catalogue des laboratoires est invalide ({reload.Issues.Count} erreur(s)).");
        }
    }

    public void Dispose() => _provider.Dispose();
}
