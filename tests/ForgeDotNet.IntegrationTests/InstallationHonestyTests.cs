using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Vérifie que l'installation livrée dit ce qu'elle fait, et qu'elle donne le moyen de faire mieux.
/// </summary>
/// <remarks>
/// <para>
/// Le produit est livré en mode manuel : il n'exécute aucun code, donc aucune preuve de maîtrise n'est
/// produite, donc aucune porte ne s'ouvre. Ce n'est pas un défaut en soi — exécuter du code soumis
/// demande un bac à sable que tout le monde n'a pas. Le défaut était que rien ne le disait : un
/// apprenant suivant le README obtenait une application où « porte A — fermée » ne distinguait pas
/// « tu n'as pas encore travaillé » de « ton installation ne peut rien valider », et où la
/// construction du bac à sable n'était documentée nulle part.
/// </para>
/// <para>
/// Ces règles portent sur des fichiers plutôt que sur du code, comme celles qui inspectent déjà le
/// Dockerfile et le workflow de laboratoire. Elles échouent si la configuration livrée change sans que
/// la documentation suive — c'est-à-dire précisément quand un lecteur serait induit en erreur.
/// </para>
/// </remarks>
public sealed class InstallationHonestyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>
    /// Le mode livré par défaut est le mode manuel, et les deux sources le disent pareil.
    /// </summary>
    [Fact]
    public void TheShippedConfigurationDefaultsToTheModeThatProducesNoProof()
    {
        using JsonDocument settings = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(RepositoryRoot, "src", "ForgeDotNet.Web", "appsettings.json")));
        string? mode = settings.RootElement.GetProperty("CodeRunner").GetProperty("Mode").GetString();

        Assert.Equal("Manual", mode);

        string compose = File.ReadAllText(Path.Combine(RepositoryRoot, "docker-compose.yml"));
        Assert.Contains("CodeRunner__Mode: Manual", compose, StringComparison.Ordinal);
    }

    /// <summary>
    /// Le mode Compose ne se rend pas paramétrable, et le fichier dit pourquoi.
    /// </summary>
    /// <remarks>
    /// Rendre cette valeur configurable serait pire que de la figer : le conteneur web n'a ni socket
    /// Docker, ni capacités, ni système de fichiers inscriptible. Une variable d'environnement
    /// laisserait croire qu'il suffit de la changer, et l'échec surviendrait plus tard, ailleurs, avec
    /// un message qui ne pourrait plus remonter jusqu'à cette décision. Le socket n'est délibérément
    /// pas monté : ce serait un chemin d'évasion vers l'hôte, contraire au modèle de menace.
    /// </remarks>
    [Fact]
    public void ComposeNeitherMountsTheDockerSocketNorPretendsItCouldExecute()
    {
        string compose = File.ReadAllText(Path.Combine(RepositoryRoot, "docker-compose.yml"));

        Assert.DoesNotContain("docker.sock", compose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CodeRunner__Mode: ${", compose, StringComparison.Ordinal);
        Assert.Contains("docs/SECURITY.md", compose, StringComparison.Ordinal);
    }

    /// <summary>
    /// La procédure qui rend l'installation validante existe, et le README y mène.
    /// </summary>
    [Fact]
    public void BuildingTheSandboxIsScriptedAndDocumented()
    {
        string scriptPath = Path.Combine(RepositoryRoot, "scripts", "build-code-runner.ps1");
        Assert.True(File.Exists(scriptPath), "Le script de construction du bac à sable est absent.");

        string script = File.ReadAllText(scriptPath);
        // Le contexte de construction n'est pas la racine du dépôt : c'est l'information qu'aucune
        // documentation ne portait, et sans laquelle la construction échoue sur un fichier introuvable.
        Assert.Contains("src/ForgeDotNet.CodeRunner/Container", script, StringComparison.Ordinal);
        Assert.Contains("sha256:[a-f0-9]{64}", script, StringComparison.Ordinal);

        string readme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));
        Assert.Contains("scripts/build-code-runner.ps1", readme, StringComparison.Ordinal);
        Assert.Contains("Valider des exercices", readme, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un message d'erreur de configuration nomme la sortie, pas seulement le format attendu.
    /// </summary>
    /// <remarks>
    /// « L'image runner doit être référencée par un identifiant immuable sha256 complet » était exact
    /// et inutilisable : la valeur manque parce que l'image n'a jamais été construite, et la
    /// construction n'était devinable d'aucune documentation. Un message de validation qui décrit une
    /// forme sans donner de chemin arrête l'installation là où elle avait besoin d'être guidée.
    /// </remarks>
    [Fact]
    public void TheStartupFailureNamesTheScriptThatResolvesIt()
    {
        string options = File.ReadAllText(Path.Combine(
            RepositoryRoot, "src", "ForgeDotNet.CodeRunner", "DockerCodeRunnerOptions.cs"));

        Assert.Contains("scripts/build-code-runner.ps1", options, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Racine du dépôt introuvable.");
    }
}
