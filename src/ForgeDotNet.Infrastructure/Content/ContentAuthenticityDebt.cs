using System.Text;
using System.Text.Json;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

/// <summary>
/// Registre explicite et versionné des documents encore hérités du générateur de contenu.
/// </summary>
/// <remarks>
/// <para>
/// C'est un cliquet, pas une exemption : un défaut d'authenticité non déclaré refuse le lot,
/// et une déclaration devenue inutile le refuse également. La dette ne peut donc que décroître,
/// et son volume reste lisible dans un seul fichier au lieu d'être dissimulé dans des tests ignorés.
/// </para>
/// <para>
/// Les entrées sont indexées par chemin <em>relatif au lot validé</em> et non à la racine du
/// contenu : un lot recopié ailleurs — ce que font les tests de catalogue — conserve ainsi la
/// même dette. La détection des déclarations périmées ne s'applique qu'aux zones nommées dans
/// le registre, car un lot dérivé peut légitimement ne pas contenir tous les documents.
/// </para>
/// </remarks>
internal sealed class ContentAuthenticityDebt
{
    public const string StaleDeclarationCode = "content-debt-stale";
    public const string InvalidRegistryCode = "content-debt-invalid";

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly Dictionary<string, HashSet<string>> _declaredByFile;
    private readonly Dictionary<string, HashSet<string>> _filesByArea;
    private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);
    private readonly string _registryRelativePath;

    private ContentAuthenticityDebt(
        Dictionary<string, HashSet<string>> declaredByFile,
        Dictionary<string, HashSet<string>> filesByArea,
        string registryRelativePath)
    {
        _declaredByFile = declaredByFile;
        _filesByArea = filesByArea;
        _registryRelativePath = registryRelativePath;
    }

    public static ContentAuthenticityDebt Empty { get; } = new(
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
        "<aucun>");

    public static ContentAuthenticityDebt Load(
        string? configuredPath,
        string contentRootPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Empty;
        }

        string fullPath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
        string relativePath = Path.GetRelativePath(contentRootPath, fullPath).Replace('\\', '/');

        if (!File.Exists(fullPath))
        {
            // Un dépôt sans dette déclarée est le cas nominal visé.
            return Empty;
        }

        var declaredByFile = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var filesByArea = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        try
        {
            using JsonDocument document = JsonDocument.Parse(
                StrictUtf8.GetString(File.ReadAllBytes(fullPath)),
                new JsonDocumentOptions { AllowTrailingCommas = false, MaxDepth = 16 });

            if (!document.RootElement.TryGetProperty("entries", out JsonElement entries)
                || entries.ValueKind != JsonValueKind.Array)
            {
                return Invalid(issues, relativePath, "$.entries",
                    "Le registre de dette doit exposer un tableau « entries ».");
            }

            foreach (JsonElement entry in entries.EnumerateArray())
            {
                if (!TryReadString(entry, "area", out string? area)
                    || !TryReadString(entry, "file", out string? file)
                    || !entry.TryGetProperty("codes", out JsonElement codes)
                    || codes.ValueKind != JsonValueKind.Array)
                {
                    return Invalid(issues, relativePath, "$.entries[]",
                        "Chaque entrée de dette exige « area », « file » et « codes ».");
                }

                string key = file!.Replace('\\', '/');
                if (!declaredByFile.TryGetValue(key, out HashSet<string>? declaredCodes))
                {
                    declaredCodes = new HashSet<string>(StringComparer.Ordinal);
                    declaredByFile.Add(key, declaredCodes);
                }

                foreach (JsonElement code in codes.EnumerateArray())
                {
                    if (code.ValueKind != JsonValueKind.String
                        || !ContentAuthenticityRules.IsAuthenticityCode(code.GetString()!))
                    {
                        return Invalid(issues, relativePath, $"$.entries[{key}].codes",
                            "Un code de dette doit appartenir aux règles d'authenticité connues.");
                    }

                    declaredCodes.Add(code.GetString()!);
                }

                if (!filesByArea.TryGetValue(area!, out HashSet<string>? areaFiles))
                {
                    areaFiles = new HashSet<string>(StringComparer.Ordinal);
                    filesByArea.Add(area!, areaFiles);
                }

                areaFiles.Add(key);
            }
        }
        catch (Exception exception) when (exception is JsonException
                                              or IOException
                                              or UnauthorizedAccessException
                                              or DecoderFallbackException)
        {
            return Invalid(issues, relativePath, "$",
                "Le registre de dette éditoriale est illisible ou mal formé.");
        }

        return new ContentAuthenticityDebt(declaredByFile, filesByArea, relativePath);
    }

    /// <summary>
    /// Absorbe un défaut d'authenticité explicitement déclaré, et le trace comme consommé.
    /// </summary>
    /// <param name="batchRelativePath">Chemin du document relatif à la racine du lot validé.</param>
    public bool Absorb(string batchRelativePath, string code)
    {
        if (!ContentAuthenticityRules.IsAuthenticityCode(code)
            || !_declaredByFile.TryGetValue(batchRelativePath, out HashSet<string>? codes)
            || !codes.Contains(code))
        {
            return false;
        }

        _consumed.Add(Key(batchRelativePath, code));
        return true;
    }

    /// <summary>
    /// Signale les déclarations devenues inutiles : le contenu a été repris, la ligne de dette
    /// doit disparaître du registre. La vérification ne s'applique qu'à une zone nommée dans le
    /// registre ; un lot dérivé ou partiel ne peut pas périmer une déclaration.
    /// </summary>
    public IEnumerable<ContentValidationIssue> GetStaleDeclarations(string areaName)
    {
        if (!_filesByArea.TryGetValue(areaName, out HashSet<string>? areaFiles))
        {
            yield break;
        }

        foreach (string file in areaFiles.OrderBy(file => file, StringComparer.Ordinal))
        {
            foreach (string code in _declaredByFile[file].OrderBy(code => code, StringComparer.Ordinal))
            {
                if (_consumed.Contains(Key(file, code)))
                {
                    continue;
                }

                yield return new ContentValidationIssue(
                    StaleDeclarationCode,
                    _registryRelativePath,
                    $"{areaName}/{file}",
                    $"La dette « {code} » déclarée pour {file} ne correspond plus à aucun défaut : "
                    + "retirer cette ligne du registre.");
            }
        }
    }

    private static ContentAuthenticityDebt Invalid(
        ICollection<ContentValidationIssue> issues,
        string relativePath,
        string propertyPath,
        string message)
    {
        issues.Add(new ContentValidationIssue(InvalidRegistryCode, relativePath, propertyPath, message));
        return Empty;
    }

    private static bool TryReadString(JsonElement element, string name, out string? value)
    {
        if (element.TryGetProperty(name, out JsonElement property)
            && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static string Key(string file, string code) => $"{file} {code}";
}
