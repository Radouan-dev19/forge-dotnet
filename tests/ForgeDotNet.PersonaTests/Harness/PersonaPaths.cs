namespace ForgeDotNet.PersonaTests.Harness;

/// <summary>Localisation du dépôt et des artefacts, partagée par tout le harnais.</summary>
internal static class PersonaPaths
{
    /// <summary>Horodatage unique de l'exécution : tous les personas d'un même run partagent le dossier.</summary>
    public static readonly string RunStamp =
        Environment.GetEnvironmentVariable("PERSONA_RUN_STAMP")
        ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>Dossier d'artefacts non versionné (ignoré par Git) : captures et registres.</summary>
    public static string ArtifactsRoot => Path.Combine(RepositoryRoot, "artifacts", "personas", RunStamp);

    public static string WebAssemblyPath => Path.Combine(
        RepositoryRoot, "src", "ForgeDotNet.Web", "bin", "Debug", "net10.0", "ForgeDotNet.Web.dll");

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Racine du dépôt introuvable depuis le dossier de test.");
    }
}
