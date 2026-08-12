using System.Text;
using System.Text.Json;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Infrastructure.SqlLab;

/// <summary>
/// Charge les scénarios SQL publiés depuis <c>content/sql</c>, énoncé et jeu de données compris.
/// </summary>
/// <remarks>
/// Les lignes attendues proviennent de <c>tests/contract.json</c>, qui reste strictement serveur :
/// aucune vue publique ne les transporte. Seuls les scénarios dont le contrat déclare le mode
/// <c>sql</c> sont exposés — les scénarios EF Core s'exécutent dans le runner isolé, pas ici.
/// </remarks>
public sealed class FileSystemSqlScenarioSource(
    SqlScenarioCatalog catalog,
    SqlScenarioContentOptions options,
    SqlLabOptions labOptions) : ISqlScenarioSource
{
    private const int MaximumFileBytes = 256 * 1024;
    private const string SqlContractMode = "sql";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private ContentCatalogProvider CatalogProvider => catalog.Provider;

    public async ValueTask<IReadOnlyList<SqlScenario>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var scenarios = new List<SqlScenario>();
        foreach (ContentCatalogItem item in CatalogProvider.Current.GetByType(ContentDocumentType.SqlScenario))
        {
            SqlScenario? scenario = await GetAsync(item.Id, cancellationToken);
            if (scenario is not null)
            {
                scenarios.Add(scenario);
            }
        }

        return Array.AsReadOnly(scenarios
            .OrderBy(scenario => scenario.Difficulty)
            .ThenBy(scenario => scenario.Id, StringComparer.Ordinal)
            .ToArray());
    }

    public async ValueTask<SqlScenario?> GetAsync(
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ContentCatalogItem? catalogItem = CatalogProvider.Current.FindById(scenarioId);
        if (catalogItem is null || catalogItem.Type != ContentDocumentType.SqlScenario)
        {
            return null;
        }

        string contentRoot = Normalize(options.ContentRootPath);
        string scenarioDirectory = Normalize(options.ScenarioDirectoryPath);
        EnsureDescendant(contentRoot, scenarioDirectory);
        string scenarioRoot = Path.GetFullPath(Path.Combine(scenarioDirectory, scenarioId));
        EnsureDescendant(scenarioDirectory, scenarioRoot);
        EnsureNoReparsePoints(contentRoot, scenarioRoot);

        string manifestText = await ReadFileAsync(contentRoot, scenarioRoot, "scenario.json", cancellationToken);
        using JsonDocument manifest = JsonDocument.Parse(manifestText, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 32,
        });

        JsonElement root = manifest.RootElement;
        if (ReadString(root, "id") != catalogItem.Id
            || root.GetProperty("version").GetInt32() != catalogItem.Version)
        {
            throw new InvalidDataException("Le scénario SQL privé ne correspond pas au catalogue public.");
        }

        string contractText = await ReadFileAsync(
            contentRoot, scenarioRoot, "tests/contract.json", cancellationToken);
        using JsonDocument contract = JsonDocument.Parse(contractText);
        if (!contract.RootElement.TryGetProperty("mode", out JsonElement mode)
            || !string.Equals(mode.GetString(), SqlContractMode, StringComparison.Ordinal))
        {
            // Scénario EF Core : il s'exécute dans le runner isolé, pas dans une session SqlLab.
            return null;
        }

        string statement = await ReadFileAsync(
            contentRoot, scenarioRoot, ReadString(root, "statementPath"), cancellationToken);
        string visibleSchema = await ReadFileAsync(
            contentRoot, scenarioRoot, ReadString(root, "visibleSchemaPath"), cancellationToken);
        string datasetSql = await ReadFileAsync(
            contentRoot, scenarioRoot, ReadString(root, "datasetPath"), cancellationToken);

        return new SqlScenario(
            catalogItem.Id,
            catalogItem.Version,
            ComputeContentRevision(manifestText, contractText, datasetSql),
            catalogItem.Title,
            root.GetProperty("difficulty").GetInt32(),
            catalogItem.Skills,
            root.GetProperty("estimatedMinutes").GetInt32(),
            statement,
            visibleSchema,
            datasetSql,
            ReadLimits(root),
            ReadStringArray(root, "effectAssertions"),
            ReadExpectation(root, contract.RootElement));
    }

    private SqlLabLimits ReadLimits(JsonElement root)
    {
        // Le scénario ne peut que resserrer les bornes du laboratoire, jamais les desserrer.
        SqlLabLimits configured = SqlLabTemplate.CreateLimits(labOptions);
        return configured with
        {
            TimeoutSeconds = Math.Min(configured.TimeoutSeconds, root.GetProperty("timeoutSeconds").GetInt32()),
            MaximumRows = Math.Min(configured.MaximumRows, root.GetProperty("maxRows").GetInt32()),
        };
    }

    private static SqlLabExpectedResult ReadExpectation(JsonElement root, JsonElement contract)
    {
        JsonElement expected = root.GetProperty("expectedResult");
        string[] columns = expected.GetProperty("columns")
            .EnumerateArray()
            .Select(column => column.GetString()!)
            .ToArray();

        var rows = new List<IReadOnlyList<SqlLabCell>>();
        if (contract.TryGetProperty("expectedRows", out JsonElement expectedRows)
            && expectedRows.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement row in expectedRows.EnumerateArray())
            {
                SqlLabCell[] cells = row.EnumerateArray()
                    .Select(cell => cell.ValueKind == JsonValueKind.Null
                        ? new SqlLabCell(null, IsNull: true)
                        : new SqlLabCell(cell.ToString()))
                    .ToArray();
                if (cells.Length != columns.Length)
                {
                    throw new InvalidDataException(
                        "Une ligne attendue du scénario SQL ne comporte pas le nombre de colonnes déclaré.");
                }

                rows.Add(Array.AsReadOnly(cells));
            }
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException("Le contrat du scénario SQL ne déclare aucune ligne attendue.");
        }

        return new SqlLabExpectedResult(
            Array.AsReadOnly(columns),
            Array.AsReadOnly(rows.ToArray()),
            expected.GetProperty("ordered").GetBoolean(),
            expected.TryGetProperty("numericTolerance", out JsonElement tolerance)
                ? tolerance.GetDecimal()
                : 0m);
    }

    /// <summary>
    /// Empreinte de l'identité exacte du contenu servi, tracée avec chaque tentative pour qu'une
    /// preuve ne survive pas à une modification du scénario.
    /// </summary>
    private static string ComputeContentRevision(string manifest, string contract, string dataset)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(
            StrictUtf8.GetBytes(string.Concat(manifest, "\n", contract, "\n", dataset)));
        return Convert.ToHexStringLower(hash);
    }

    private static async ValueTask<string> ReadFileAsync(
        string contentRoot,
        string scenarioRoot,
        string relativePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath)
            || relativePath.Split('/', '\\').Any(segment => segment is "." or ".."))
        {
            throw new InvalidDataException($"Chemin de scénario SQL refusé : {relativePath}");
        }

        string fullPath = Path.GetFullPath(Path.Combine(
            scenarioRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        EnsureDescendant(scenarioRoot, fullPath);
        EnsureNoReparsePoints(contentRoot, fullPath);

        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Fichier de scénario SQL introuvable.", relativePath);
        }

        if (fileInfo.Length > MaximumFileBytes)
        {
            throw new InvalidDataException($"Fichier de scénario SQL trop volumineux : {relativePath}");
        }

        return StrictUtf8.GetString(await File.ReadAllBytesAsync(fullPath, cancellationToken));
    }

    private static string ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new InvalidDataException($"Propriété « {propertyName} » absente du scénario SQL.");

    private static System.Collections.ObjectModel.ReadOnlyCollection<string> ReadStringArray(
        JsonElement root,
        string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.Array
            ? Array.AsReadOnly(value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray())
            : [];

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static void EnsureDescendant(string root, string candidate)
    {
        string normalizedRoot = Normalize(root);
        string normalizedCandidate = Path.GetFullPath(candidate);
        bool within = normalizedCandidate.Equals(normalizedRoot, PathComparison)
            || normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, PathComparison);
        if (!within)
        {
            throw new InvalidDataException("Le chemin de scénario SQL sort de son dossier autorisé.");
        }
    }

    private static void EnsureNoReparsePoints(string root, string candidate)
    {
        string current = Normalize(root);
        foreach (string segment in Path.GetRelativePath(current, candidate).Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Lien symbolique interdit dans un scénario SQL.");
            }
        }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
