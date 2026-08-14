using System.Text.Json;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Fige l'accessibilité réelle des cinq scénarios EF Core, qui n'est pas celle qu'on suppose.
/// </summary>
/// <remarks>
/// <para>
/// L'exigence « EF Core » de la porte B paraît branchable sur les validations du laboratoire SQL :
/// cinq scénarios <c>ef-*</c> sont publiés, ils exécutent du vrai code EF Core, et leur validation est
/// comparée côté serveur. C'est faux, pour deux raisons qui ne se voient dans aucun fichier de contenu.
/// </para>
/// <para>
/// D'abord <c>FileSystemSqlScenarioSource</c> n'expose que les scénarios dont le contrat déclare le
/// mode <c>sql</c> ; les scénarios EF déclarent le mode <c>ef</c> et sont donc absents du laboratoire.
/// Ensuite un scénario EF n'est tirable en examen que s'il porte un dossier <c>exam/</c>, et trois des
/// cinq n'en ont pas. Le résultat est qu'aucun chemin du produit ne permet à un apprenant de valider
/// les cinq, donc qu'aucune preuve d'EF Core n'est collectable aujourd'hui.
/// </para>
/// <para>
/// Ce test existe pour que la prochaine tentative de brancher cette exigence trouve le diagnostic déjà
/// écrit, au lieu de le redécouvrir. Il échouera dès que l'accessibilité changera, ce qui est
/// exactement le moment où l'exigence redevient branchable.
/// </para>
/// </remarks>
[Trait("Category", "MasteryReachability")]
public sealed class EfScenarioReachabilityTests
{
    private static readonly string ContentRoot = Path.Combine(FindRepositoryRoot(), "content");

    /// <summary>Mode de contrat qu'expose le laboratoire SQL, à l'exclusion de tout autre.</summary>
    private const string LabContractMode = "sql";

    /// <summary>Mode de contrat des scénarios qui s'exécutent dans le runner isolé.</summary>
    private const string RunnerContractMode = "ef";

    [Fact]
    public void EveryEfScenarioIsExcludedFromTheSqlLabByItsContractMode()
    {
        string[] efScenarios = EfScenarioDirectories();

        Assert.Equal(5, efScenarios.Length);
        Assert.All(efScenarios, directory =>
        {
            string mode = ContractMode(directory);
            Assert.Equal(RunnerContractMode, mode);
            Assert.NotEqual(LabContractMode, mode);
        });
    }

    /// <summary>
    /// Trois des cinq scénarios EF ne portent pas le dossier qu'un examen exige pour les tirer.
    /// </summary>
    /// <remarks>
    /// <c>FileSystemExamBankSource.LoadEfCandidateAsync</c> lit <c>exam/starter/Submission.cs</c>,
    /// <c>exam/solution/Submission.cs</c> et <c>exam/tests/</c>. Sans ce dossier, le scénario ne peut
    /// pas figurer dans une banque : il est publié, validé par le validateur de contenu, et inaccessible.
    /// </remarks>
    [Fact]
    public void ThreeEfScenariosCannotBeDrawnByAnyExam()
    {
        string[] withExamFolder = EfScenarioDirectories()
            .Where(directory => Directory.Exists(Path.Combine(directory, "exam")))
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray()!;

        Assert.Equal(["ef-orders-queryable-001", "ef-orders-tracking-001"], withExamFolder);
    }

    /// <summary>
    /// Aucune banque d'examen ne cite un scénario EF dépourvu du dossier requis.
    /// </summary>
    /// <remarks>
    /// Le contrôle porte dans l'autre sens que le précédent : il refuse qu'une banque déclare éligible
    /// un scénario que le chargeur ne saurait pas lire, ce qui ferait échouer la banque entière au
    /// démarrage de l'application plutôt qu'à la revue.
    /// </remarks>
    [Fact]
    public void NoExamBankDeclaresAnEfScenarioItCannotLoad()
    {
        string[] declared = Directory
            .EnumerateFiles(Path.Combine(ContentRoot, "exams"), "exam.json", SearchOption.AllDirectories)
            .SelectMany(EligibleEfScenarioIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(declared);
        Assert.All(declared, id => Assert.True(
            Directory.Exists(Path.Combine(ContentRoot, "sql", id, "exam")),
            $"{id} est déclaré éligible sans porter le dossier exam/ que le chargeur exige."));
    }

    private static string[] EfScenarioDirectories() => Directory
        .EnumerateDirectories(Path.Combine(ContentRoot, "sql"), "ef-*")
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    private static string ContractMode(string scenarioDirectory)
    {
        using JsonDocument contract = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(scenarioDirectory, "tests", "contract.json")));
        return contract.RootElement.GetProperty("mode").GetString()!;
    }

    private static IEnumerable<string> EligibleEfScenarioIds(string manifestPath)
    {
        using JsonDocument exam = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        if (!exam.RootElement.TryGetProperty("eligibleEfScenarioIds", out JsonElement eligible))
        {
            yield break;
        }

        foreach (JsonElement item in eligible.EnumerateArray())
        {
            yield return item.GetString()!;
        }
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
