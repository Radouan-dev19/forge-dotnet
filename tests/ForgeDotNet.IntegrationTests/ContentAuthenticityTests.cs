using System.Text.Json;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Infrastructure.Content;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Cliquet sur la dette éditoriale héritée du générateur de contenu.
/// </summary>
/// <remarks>
/// Les schémas et les tests d'exécution acceptaient soixante-dix leçons clonées : ils
/// contrôlaient la structure, jamais l'authenticité. Ces tests refusent tout défaut
/// d'authenticité non déclaré, toute déclaration devenue inutile, et toute dette supérieure au
/// plafond figé ci-dessous. Baisser un plafond est une décision humaine explicite ; l'augmenter
/// exige de modifier ce fichier, ce qui reste visible dans la revue.
/// </remarks>
public sealed class ContentAuthenticityTests
{
    /// <summary>
    /// Documents encore couverts par le registre. Relevé initial : 376, aujourd'hui zéro.
    /// </summary>
    /// <remarks>
    /// Descente, lot par lot : 376 au relevé initial, puis 164 après la reprise des leçons, des
    /// exercices et des DebugLabs, 159 après celle des briefs de projet, 131 après les vingt-huit
    /// scénarios SQL, 106 après les cinquante cartes d'anglais, et zéro après les cent
    /// quatre-vingt-onze fiches d'entretien.
    ///
    /// À zéro, le cliquet change de nature : il ne borne plus une dette héritée, il interdit toute
    /// réapparition. Le premier paragraphe recopié dans plus de trois documents d'un même lot fait
    /// désormais échouer ce test, sans qu'aucune déclaration ne puisse l'absorber — ce qui est le
    /// seul état où la règle protège le contenu neuf autant que l'ancien.
    /// </remarks>
    private const int MaximumDebtedDocuments = 0;

    /// <summary>
    /// Couples (document, règle) encore tolérés. Relevé initial : 667, aujourd'hui zéro.
    /// </summary>
    /// <remarks>
    /// Les trois règles d'authenticité — marqueur non substitué, contenu recopié, leçon creuse —
    /// n'ont plus aucune exception déclarée. Une régression sur l'une des trois est donc un échec de
    /// build, jamais une ligne à ajouter au registre.
    /// </remarks>
    private const int MaximumDeclaredRuleViolations = 0;

    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ContentRoot = Path.Combine(RepositoryRoot, "content");

    [Theory]
    [InlineData("reference")]
    [InlineData("sql")]
    public async Task PublishedContentCarriesNoUndeclaredAuthenticityDefect(string area)
    {
        var validator = new FileSystemContentValidationService(new ContentValidationOptions
        {
            ContentRootPath = ContentRoot,
        });

        ContentValidationReport report = await validator.ValidateAsync(Path.Combine(ContentRoot, area));

        Assert.True(
            report.IsValid,
            "Défaut d'authenticité non déclaré, ou déclaration devenue inutile dans le registre :"
            + Environment.NewLine
            + string.Join(
                Environment.NewLine,
                report.Issues.Select(issue => $"{issue.FilePath} | {issue.Code} | {issue.Message}")));
    }

    [Fact]
    public async Task InheritedContentDebtNeverGrowsBeyondItsRecordedCeiling()
    {
        (int documents, int violations, Dictionary<string, int> byRule) = await ReadRegistryAsync();

        string breakdown = string.Join(
            ", ",
            byRule.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"));

        Assert.True(
            documents <= MaximumDebtedDocuments,
            $"La dette éditoriale a augmenté : {documents} document(s) déclarés pour un plafond de "
            + $"{MaximumDebtedDocuments}. Répartition : {breakdown}.");
        Assert.True(
            violations <= MaximumDeclaredRuleViolations,
            $"La dette éditoriale a augmenté : {violations} déclaration(s) pour un plafond de "
            + $"{MaximumDeclaredRuleViolations}. Répartition : {breakdown}.");
    }

    [Fact]
    public async Task EveryDeclaredDebtNamesAnExistingDocumentAndAKnownRule()
    {
        (_, _, Dictionary<string, int> byRule) = await ReadRegistryAsync();

        Assert.All(byRule.Keys, code => Assert.True(
            ContentAuthenticityRules.IsAuthenticityCode(code),
            $"Le registre déclare une règle inconnue : {code}."));

        using JsonDocument registry = JsonDocument.Parse(
            await File.ReadAllTextAsync(RegistryPath()));
        foreach (JsonElement entry in registry.RootElement.GetProperty("entries").EnumerateArray())
        {
            string path = $"{entry.GetProperty("area").GetString()}/{entry.GetProperty("file").GetString()}";
            Assert.True(
                File.Exists(Path.Combine(ContentRoot, path.Replace('/', Path.DirectorySeparatorChar))),
                $"Le registre déclare une dette sur un document absent : {path}.");
        }
    }

    private static string RegistryPath() => Path.Combine(
        ContentRoot,
        ContentValidationOptions.DefaultLegacyDebtFileName.Replace('/', Path.DirectorySeparatorChar));

    private static async Task<(int Documents, int Violations, Dictionary<string, int> ByRule)> ReadRegistryAsync()
    {
        using JsonDocument registry = JsonDocument.Parse(await File.ReadAllTextAsync(RegistryPath()));
        var byRule = new Dictionary<string, int>(StringComparer.Ordinal);
        int documents = 0;
        int violations = 0;
        foreach (JsonElement entry in registry.RootElement.GetProperty("entries").EnumerateArray())
        {
            documents++;
            foreach (JsonElement code in entry.GetProperty("codes").EnumerateArray())
            {
                violations++;
                string value = code.GetString()!;
                byRule[value] = byRule.GetValueOrDefault(value) + 1;
            }
        }

        return (documents, violations, byRule);
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
