using ForgeDotNet.Application.Content;
using ForgeDotNet.Infrastructure.Content;

namespace ForgeDotNet.Infrastructure.SqlLab;

/// <summary>
/// Snapshot immuable des scénarios SQL publiés, distinct du catalogue du lecteur.
/// </summary>
/// <remarks>
/// Les scénarios vivent sous <c>content/sql</c> et non sous <c>content/reference</c> : ils ont donc
/// leur propre catalogue validé, chargé atomiquement au démarrage. Sans ce chargement, les quarante
/// scénarios livrés restaient invisibles du produit — c'est exactement le défaut P1-03 de l'audit.
/// </remarks>
public sealed class SqlScenarioCatalog : IDisposable
{
    private readonly ContentCatalogProvider _provider;

    public SqlScenarioCatalog(ContentValidationOptions validationOptions, string scenarioDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(validationOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioDirectoryPath);
        ScenarioDirectoryPath = scenarioDirectoryPath;
        _provider = new ContentCatalogProvider(new FileSystemContentCatalogLoader(
            new FileSystemContentValidationService(validationOptions),
            validationOptions));
    }

    public string ScenarioDirectoryPath { get; }

    public ContentCatalogProvider Provider => _provider;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ContentCatalogReloadResult reload = await _provider.ReloadAsync(ScenarioDirectoryPath, cancellationToken);
        if (!reload.Succeeded)
        {
            throw new InvalidDataException(
                $"Le catalogue des scénarios SQL est invalide ({reload.Issues.Count} erreur(s)).");
        }
    }

    public void Dispose() => _provider.Dispose();
}
