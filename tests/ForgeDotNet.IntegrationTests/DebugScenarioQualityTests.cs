using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// Refuse le retour d'un matériel de diagnostic générique sur les vingt-cinq DebugLabs publiés.
/// </summary>
/// <remarks>
/// Dix-sept scénarios portaient un ticket qui nommait la cause — « La division utilise une longueur
/// nulle » —, un journal réduit à une ligne ne contenant que l'identifiant du scénario et deux
/// constantes, et une rubrique identique dont les termes attendus étaient « borne, condition,
/// mutation ». Les leçons <c>debug-stacktraces-breakpoints-001</c> et
/// <c>performance-security-incident-001</c> enseignent une méthode en quatre temps : symptôme,
/// hypothèse, preuve, prévention. Ce matériel court-circuitait les trois premiers, et l'évaluation
/// ne pouvait pas distinguer un journal diagnostiqué d'un journal rempli de mots passe-partout.
///
/// Ces tests figent l'état atteint. Ils ne jugent pas la qualité rédactionnelle — ce que seule une
/// relecture humaine peut faire — mais les quatre propriétés mécaniques qui échouaient toutes sur
/// exactement ces dix-sept scénarios.
/// </remarks>
public sealed partial class DebugScenarioQualityTests
{
    /// <summary>
    /// Seuil de la règle <c>cloned-content</c> : un texte partagé par plus de trois documents du lot
    /// est un texte recopié. Appliqué ici au matériel que l'apprenant lit avant de diagnostiquer.
    /// </summary>
    private const int MaximumSharingScenarios = 3;

    /// <summary>
    /// Un journal doit porter au moins deux mesures : sans elles, il ne reste que l'identifiant du
    /// scénario, qui n'apprend rien et répète ce que l'apprenant sait déjà.
    /// </summary>
    private const int MinimumMeasurementsPerLog = 2;

    private static readonly string DebuggingRoot = Path.Combine(
        FindRepositoryRoot(), "content", "reference", "debugging");

    /// <summary>
    /// Clés d'entête : elles situent la trace mais ne mesurent rien. Le journal générique n'en
    /// portait que celles-là, plus deux constantes.
    /// </summary>
    private static readonly string[] MetadataKeys = ["Event", "Level", "Scenario", "CorrelationId", "Week"];

    /// <summary>Valeurs qui ne constituent pas une mesure : elles ne varient pas d'un cas à l'autre.</summary>
    private static readonly string[] EmptyMeasurements = ["MISMATCH", "CONTRACT", "none", "true", "false"];

    [Theory]
    [InlineData("ticket")]
    [InlineData("logs")]
    [InlineData("expectedBehavior")]
    [InlineData("rubric")]
    public void NoDiagnosticMaterialIsSharedByMoreThanThreeScenarios(string material)
    {
        string[] shared = ReadScenarios()
            .GroupBy(scenario => Select(scenario, material), StringComparer.Ordinal)
            .Where(group => group.Count() > MaximumSharingScenarios)
            .Select(group => $"« {Truncate(group.Key)} » partagé par "
                + string.Join(", ", group.Select(scenario => scenario.Id).OrderBy(id => id, StringComparer.Ordinal)))
            .ToArray();

        Assert.True(shared.Length == 0, $"Matériel « {material} » recopié :\n{string.Join('\n', shared)}");
    }

    [Fact]
    public void EveryLogFileCarriesAtLeastTwoMeasurements()
    {
        var offenders = new List<string>();

        foreach (Scenario scenario in ReadScenarios())
        {
            int measurements = MeasurementRegex()
                .Matches(scenario.Logs)
                .Count(match => IsMeasurement(
                    match.Groups["key"].Value,
                    match.Groups["value"].Value,
                    scenario.Id));

            if (measurements < MinimumMeasurementsPerLog)
            {
                offenders.Add($"{scenario.Id} : {measurements} mesure(s) exploitable(s) dans le journal.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    [Fact]
    public void NoRegressionNoteReferencesATruncatedSourcePath()
    {
        // Le générateur écrivait « roken/Submission.cs » : le « b » disparaissait à l'échappement.
        string[] offenders = ReadScenarios()
            .Where(scenario => TruncatedPathRegex().IsMatch(scenario.RegressionTest))
            .Select(scenario => $"{scenario.Id} : chemin de source tronqué dans la note de non-régression.")
            .ToArray();

        Assert.True(offenders.Length == 0, string.Join('\n', offenders));
    }

    /// <remarks>
    /// La règle porte sur le <b>ticket</b> seul. Le comportement attendu est le contrat : il doit
    /// pouvoir énoncer la valeur de repli exacte, ce qui est son rôle. Le ticket, lui, rapporte un
    /// symptôme observé et ne doit jamais nommer la construction que la correction introduit.
    /// </remarks>
    [Fact]
    public void NoTicketNamesAnIdentifierIntroducedByItsOwnCorrection()
    {
        var offenders = new List<string>();

        foreach (Scenario scenario in ReadScenarios())
        {
            string ticket = scenario.Ticket.ToLowerInvariant();

            foreach (string identifier in AddedIdentifiers(scenario.Id))
            {
                if (Regex.IsMatch(ticket, $@"\b{Regex.Escape(identifier)}\b", RegexOptions.CultureInvariant))
                {
                    offenders.Add($"{scenario.Id} : le ticket nomme « {identifier} », introduit par la correction.");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    private static bool IsMeasurement(string key, string value, string scenarioId) =>
        !MetadataKeys.Contains(key, StringComparer.OrdinalIgnoreCase)
        && !value.Contains(scenarioId, StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("dbg-", StringComparison.OrdinalIgnoreCase)
        && !EmptyMeasurements.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> AddedIdentifiers(string scenarioId)
    {
        string brokenPath = Path.Combine(DebuggingRoot, scenarioId, "broken", "Submission.cs");
        string correctionPath = Path.Combine(DebuggingRoot, scenarioId, "correction", "Submission.cs");
        if (!File.Exists(brokenPath) || !File.Exists(correctionPath))
        {
            return [];
        }

        HashSet<string> broken = IdentifierRegex()
            .Matches(File.ReadAllText(brokenPath))
            .Select(match => match.Value.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return IdentifierRegex()
            .Matches(File.ReadAllText(correctionPath))
            .Select(match => match.Value.ToLowerInvariant())
            .Where(identifier => !broken.Contains(identifier))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string Select(Scenario scenario, string material) => material switch
    {
        "ticket" => scenario.Ticket,
        "logs" => scenario.Logs,
        "expectedBehavior" => scenario.ExpectedBehavior,
        "rubric" => scenario.RubricSignature,
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Matériel inconnu."),
    };

    private static IEnumerable<Scenario> ReadScenarios()
    {
        foreach (string directory in Directory.GetDirectories(DebuggingRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            string manifestPath = Path.Combine(directory, "scenario.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            using JsonDocument rubric = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(directory, "tests", "rubric.json")));

            string signature = string.Join(" | ", rubric.RootElement.GetProperty("criteria").EnumerateArray()
                .Select(criterion => criterion.GetProperty("id").GetString()
                    + ":" + string.Join(',', criterion.GetProperty("requiredTerms").EnumerateArray()
                        .Select(term => term.GetString()))));

            yield return new Scenario(
                Path.GetFileName(directory),
                File.ReadAllText(Path.Combine(directory, "ticket.md")),
                File.ReadAllText(Path.Combine(directory, "logs.txt")),
                manifest.RootElement.GetProperty("expectedBehavior").GetString()!,
                File.ReadAllText(Path.Combine(directory, "regression-test.md")),
                signature);
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 90 ? value.ReplaceLineEndings(" ") : value[..90].ReplaceLineEndings(" ") + "…";

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

    [GeneratedRegex(@"(?<key>\w+)=(?<value>[^\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex MeasurementRegex();

    [GeneratedRegex(@"(?<![A-Za-z])roken/Submission", RegexOptions.CultureInvariant)]
    private static partial Regex TruncatedPathRegex();

    [GeneratedRegex(@"[A-Za-z]{4,}", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    private sealed record Scenario(
        string Id,
        string Ticket,
        string Logs,
        string ExpectedBehavior,
        string RegressionTest,
        string RubricSignature);
}
