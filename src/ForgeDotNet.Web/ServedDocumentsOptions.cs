namespace ForgeDotNet.Web;

/// <summary>
/// Documents du dépôt que l'application sert en lecture seule sur <c>/docs/{nom}</c>.
/// </summary>
/// <remarks>
/// Plusieurs pages nommaient un fichier — <c>docs/HUMAN_REVIEW.md</c>, <c>docs/HUMAN_PANEL_KIT.md</c>,
/// la section « Valider des exercices » du README — que l'apprenant devait aller chercher dans un dépôt
/// qu'il n'a pas forcément (déploiement Compose). La liste est fermée et le nom demandé n'entre jamais
/// dans la composition d'un chemin : seul un nom exactement listé est résolu.
/// </remarks>
public sealed class ServedDocumentsOptions
{
    private readonly Dictionary<string, string> _paths;

    public ServedDocumentsOptions(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        _paths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["HUMAN_REVIEW.md"] = Path.Combine(repositoryRoot, "docs", "HUMAN_REVIEW.md"),
            ["HUMAN_PANEL_KIT.md"] = Path.Combine(repositoryRoot, "docs", "HUMAN_PANEL_KIT.md"),
            ["SQLLAB.md"] = Path.Combine(repositoryRoot, "docs", "SQLLAB.md"),
            ["RUNBOOK.md"] = Path.Combine(repositoryRoot, "docs", "RUNBOOK.md"),
            ["README.md"] = Path.Combine(repositoryRoot, "README.md"),
        };
        ExportCareerEvidenceScriptPath = Path.Combine(
            repositoryRoot, "content", "reference", "career", "Export-CareerEvidence.ps1");
    }

    /// <summary>Script d'export des preuves de carrière, proposé en téléchargement.</summary>
    public string ExportCareerEvidenceScriptPath { get; }

    public IReadOnlyCollection<string> Names => _paths.Keys;

    /// <summary>Chemin absolu du document, ou <see langword="null"/> s'il n'est pas listé ou absent.</summary>
    public string? Resolve(string? name) =>
        name is not null && _paths.TryGetValue(name, out string? path) && File.Exists(path) ? path : null;
}
