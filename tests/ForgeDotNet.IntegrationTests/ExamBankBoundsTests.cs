using System.Text;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Exams;
using ForgeDotNet.Infrastructure.Practice;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Fige les bornes des listes d'une banque d'examen, et surtout la raison de leur écart.
/// </summary>
/// <remarks>
/// <para>
/// Une borne commune à seize entrées s'appliquait au vivier d'éligibilité comme aux contraintes d'un
/// item. Le vivier grandit avec le catalogue ; il avait donc atteint son plafond, et tout exercice
/// publié ensuite devenait intirable — un exercice hors banque n'alimente ni la composante « examen
/// sans aide » ni la porte qui en dépend. Aucun test ne couvrait cette borne, si bien que l'ajout
/// d'un exercice au vivier faisait refuser la banque entière <b>au démarrage</b> de l'application,
/// loin du fichier fautif.
/// </para>
/// <para>
/// Ces deux tests documentent l'intention : le vivier accepte largement plus que la taille actuelle
/// du catalogue, et une liste absurde est toujours refusée à la lecture du manifeste.
/// </para>
/// </remarks>
[Trait("Category", "ExamIntegrity")]
public sealed class ExamBankBoundsTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>
    /// Le vivier publié dépasse déjà l'ancienne borne : ce test échouerait si elle revenait.
    /// </summary>
    [Fact]
    public async Task PublishedBanksLoadEvenWhenTheirEligibleListExceedsSixteenItems()
    {
        string contentRoot = Path.Combine(RepositoryRoot, "content");
        using var provider = CreateProvider();
        await LoadCatalogAsync(provider, contentRoot);

        FileSystemExamBankSource bank = CreateBank(provider, contentRoot, contentRoot);
        IReadOnlyList<ExamBlueprint> blueprints = await bank.ListAsync();

        Assert.Equal(10, blueprints.Count);
        ExamBlueprint apiExam = Assert.Single(blueprints, item => item.Id == "api-security-v1");
        Assert.True(
            apiExam.Candidates.Count > 16,
            $"Le vivier de l'examen API compte {apiExam.Candidates.Count} candidats : la borne de "
            + "seize est de retour, et tout exercice publié au-delà redevient intirable.");

        // Le tirage reste petit devant le vivier : c'est précisément ce que la borne unique confondait.
        Assert.True(apiExam.DrawCount < apiExam.Candidates.Count);
    }

    /// <summary>
    /// La borne reste une borne : une liste absurde est refusée, et elle l'est à la lecture du
    /// manifeste, avant toute résolution d'exercice.
    /// </summary>
    [Fact]
    public async Task AnEligibleListBeyondItsBoundIsRefusedWhileReadingTheManifest()
    {
        string contentRoot = Path.Combine(RepositoryRoot, "content");
        string oversizedRoot = Path.Combine(
            Path.GetTempPath(),
            "ForgeDotNet.ExamBankBounds",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(oversizedRoot, "exams", "oversized-v1"));
        try
        {
            string[] identifiers = [.. Enumerable.Range(1, 257).Select(index => $"\"exercise-{index:D4}-001\"")];
            await File.WriteAllTextAsync(
                Path.Combine(oversizedRoot, "exams", "oversized-v1", "exam.json"),
                $$"""
                {
                  "schemaVersion": 1,
                  "id": "oversized-v1",
                  "version": 1,
                  "title": "Vivier hors borne",
                  "durationMinutes": 60,
                  "drawCount": 4,
                  "passingScore": 80,
                  "eligibleExerciseIds": [{{string.Join(",", identifiers)}}]
                }
                """,
                new UTF8Encoding(false));

            using var provider = CreateProvider();
            await LoadCatalogAsync(provider, contentRoot);
            FileSystemExamBankSource bank = CreateBank(provider, contentRoot, oversizedRoot);

            InvalidDataException failure = await Assert.ThrowsAsync<InvalidDataException>(
                async () => await bank.ListAsync());
            Assert.Contains("liste", failure.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(oversizedRoot, recursive: true);
        }
    }

    private static ContentCatalogProvider CreateProvider()
    {
        var options = new ContentValidationOptions
        {
            ContentRootPath = Path.Combine(RepositoryRoot, "content"),
        };
        return new ContentCatalogProvider(new FileSystemContentCatalogLoader(
            new FileSystemContentValidationService(options),
            options));
    }

    private static async Task LoadCatalogAsync(ContentCatalogProvider provider, string contentRoot)
    {
        ContentCatalogReloadResult reload = await provider.ReloadAsync(Path.Combine(contentRoot, "reference"));
        Assert.True(reload.Succeeded, string.Join(Environment.NewLine, reload.Issues.Select(item => item.Message)));
    }

    /// <summary>
    /// Le catalogue d'exercices reste celui du dépôt ; seule la racine des banques change, ce qui
    /// permet d'éprouver un manifeste hors borne sans fabriquer un catalogue complet.
    /// </summary>
    private static FileSystemExamBankSource CreateBank(
        ContentCatalogProvider provider,
        string catalogContentRoot,
        string bankContentRoot) => new(
        new FileSystemPracticeExerciseSource(provider, new PracticeContentOptions
        {
            ContentRootPath = catalogContentRoot,
            CatalogDirectoryPath = Path.Combine(catalogContentRoot, "reference"),
        }),
        new ExamBankOptions
        {
            ContentRootPath = bankContentRoot,
            BankDirectoryPath = Path.Combine(bankContentRoot, "exams"),
        });

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
