using System.Text.Json;
using ForgeDotNet.Application.Labs;
using ForgeDotNet.Domain.Labs;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Labs;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Prouve que le catalogue des laboratoires se charge, que ses renvois vers le catalogue de référence
/// existent, et qu'un manifeste invalide est refusé plutôt que dégradé.
/// </summary>
/// <remarks>
/// Les laboratoires vivent sous <c>content/labs</c>, hors du catalogue du lecteur, dans un snapshot
/// propre — le même patron que les scénarios SQL. Deux conséquences à vérifier ici : le champ
/// <c>recommendedBefore</c> ne peut pas être résolu par son propre catalogue, donc son intégrité se
/// contrôle entre les deux catalogues ; et un manifeste invalide doit refuser le chargement entier,
/// puisque l'application le charge au démarrage.
/// </remarks>
[Trait("Category", "LabContent")]
public sealed class LabContentTests
{
    private static readonly string ContentRoot = Path.Combine(FindRepositoryRoot(), "content");

    [Fact]
    public async Task TheLabCatalogueLoadsItsElevenManifests()
    {
        using LabCatalog catalog = CreateCatalog();
        await catalog.LoadAsync();
        using var source = new FileSystemLabSource(catalog, Options());

        IReadOnlyList<Lab> labs = await source.ListAsync();

        Assert.Equal(11, labs.Count);
        Assert.All(labs, lab => Assert.True(lab.IsLearnerDeclared, lab.Id));
    }

    /// <summary>
    /// Chaque renvoi « à travailler avant » désigne un contenu réellement publié dans le catalogue de
    /// référence.
    /// </summary>
    /// <remarks>
    /// C'est le contrôle que le chargeur ne peut pas faire lui-même : il ne voit que
    /// <c>content/labs</c>. Sans ce test, une leçon renommée casserait silencieusement le renvoi, et
    /// l'apprenant suivrait un lien mort.
    /// </remarks>
    [Fact]
    public async Task EveryRecommendedContentExistsInTheReferenceCatalogue()
    {
        using LabCatalog catalog = CreateCatalog();
        await catalog.LoadAsync();
        using var source = new FileSystemLabSource(catalog, Options());
        HashSet<string> published = PublishedReferenceIds();

        foreach (Lab lab in await source.ListAsync())
        {
            Assert.All(lab.Prerequisites, id => Assert.True(
                published.Contains(id),
                $"{lab.Id} recommande « {id} », introuvable dans content/reference."));
        }
    }

    /// <summary>
    /// Un manifeste invalide refuse le catalogue entier au chargement, jamais silencieusement.
    /// </summary>
    [Fact]
    public async Task AnInvalidManifestRefusesTheWholeCatalogue()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ForgeDotNet.LabContentTests",
            Guid.NewGuid().ToString("N"));
        string labDirectory = Path.Combine(root, "labs", "broken-lab");
        Directory.CreateDirectory(labDirectory);
        try
        {
            // La politique de preuve déclarée n'est pas celle que le schéma impose : c'est le
            // mensonge exact que la valeur constante existe pour empêcher.
            await File.WriteAllTextAsync(
                Path.Combine(labDirectory, "lab.json"),
                """
                {
                  "schemaVersion": 1,
                  "id": "broken-lab",
                  "version": 1,
                  "title": "Laboratoire qui prétend prouver",
                  "weeks": [11],
                  "skills": ["api.contracts"],
                  "recommendedBefore": [],
                  "estimatedMinutes": 60,
                  "briefPath": "README.md",
                  "objectives": [
                    { "id": "one", "goal": "Un objectif assez long pour le schéma.", "observableProof": "Une preuve assez longue pour le schéma." },
                    { "id": "two", "goal": "Un second objectif assez long aussi.", "observableProof": "Une seconde preuve assez longue aussi." }
                  ],
                  "commands": [
                    { "shell": "dotnet", "command": "dotnet build broken.csproj", "purpose": "Construire le projet du laboratoire cassé." }
                  ],
                  "limits": ["Une limite déclarée assez longue pour le schéma."],
                  "evidencePolicy": "server-verified",
                  "license": "CC-BY-4.0"
                }
                """,
                new System.Text.UTF8Encoding(false));
            await File.WriteAllTextAsync(
                Path.Combine(labDirectory, "README.md"),
                "# Laboratoire cassé\n\nBrief factice du laboratoire invalide.\n",
                new System.Text.UTF8Encoding(false));

            using var catalog = new LabCatalog(
                new ContentValidationOptions { ContentRootPath = root },
                Path.Combine(root, "labs"));

            InvalidDataException failure = await Assert.ThrowsAsync<InvalidDataException>(
                () => catalog.LoadAsync());
            Assert.Contains("invalide", failure.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static LabCatalog CreateCatalog() => new(
        new ContentValidationOptions { ContentRootPath = ContentRoot },
        Path.Combine(ContentRoot, "labs"));

    private static LabContentOptions Options() => new()
    {
        ContentRootPath = ContentRoot,
        LabDirectoryPath = Path.Combine(ContentRoot, "labs"),
    };

    /// <summary>
    /// Identifiants publiés du catalogue de référence, lus depuis les manifestes eux-mêmes.
    /// </summary>
    private static HashSet<string> PublishedReferenceIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string manifest in Directory.EnumerateFiles(
            Path.Combine(ContentRoot, "reference"), "*.json", SearchOption.AllDirectories))
        {
            // Les artefacts privés d'un contenu — starter, solution, cas de test — ne portent pas
            // d'identifiant publiable ; le classificateur du produit les ignore de la même façon.
            string normalized = Path.GetRelativePath(ContentRoot, manifest).Replace('\\', '/');
            if (normalized.Contains("/starter/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/solution/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(manifest));
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("id", out JsonElement id)
                    && id.ValueKind == JsonValueKind.String)
                {
                    ids.Add(id.GetString()!);
                }
            }
            catch (JsonException)
            {
                // Les fichiers non-manifestes du catalogue ne portent pas d'identifiant.
            }
        }

        return ids;
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Racine du dépôt de test introuvable.");
    }
}
