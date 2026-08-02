using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

public sealed partial class FileSystemContentValidationService : IContentValidationService
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly string _contentRootPath;
    private readonly int _maximumFiles;
    private readonly long _maximumFileSizeBytes;
    private readonly string _schemaRootPath;

    public FileSystemContentValidationService(ContentValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ContentRootPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumFileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumFiles);

        _contentRootPath = NormalizeDirectory(options.ContentRootPath);
        _schemaRootPath = NormalizeDirectory(
            options.SchemaRootPath ?? Path.Combine(_contentRootPath, "schemas"));
        _maximumFileSizeBytes = options.MaximumFileSizeBytes;
        _maximumFiles = options.MaximumFiles;

        if (!IsWithin(_contentRootPath, _schemaRootPath))
        {
            throw new InvalidOperationException("Le dossier des schémas doit rester sous la racine content.");
        }
    }

    public Task<ContentValidationReport> ValidateAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<ContentValidationIssue>();
        string targetPath = NormalizeDirectory(directoryPath);
        if (!IsWithin(_contentRootPath, targetPath))
        {
            AddIssue(issues, "path-outside-content", "<racine>", "$", "Le dossier à valider doit rester sous content/.");
            return Task.FromResult(new ContentValidationReport(0, 0, issues));
        }

        if (!Directory.Exists(targetPath))
        {
            AddIssue(issues, "directory-not-found", Relative(targetPath), "$", "Dossier de contenu introuvable.");
            return Task.FromResult(new ContentValidationReport(0, 0, issues));
        }

        if (ContainsReparsePointBetween(_contentRootPath, targetPath))
        {
            AddIssue(issues, "reparse-point", Relative(targetPath), "$", "Lien symbolique ou point de réanalyse interdit dans le chemin de validation.");
            return Task.FromResult(new ContentValidationReport(0, 0, issues));
        }

        List<string> files = EnumerateFilesSafely(targetPath, issues, cancellationToken);
        if (files.Count > _maximumFiles)
        {
            AddIssue(issues, "too-many-files", Relative(targetPath), "$", $"Le lot dépasse la limite de {_maximumFiles} fichiers.");
            return Task.FromResult(new ContentValidationReport(files.Count, 0, issues));
        }

        Dictionary<ContentDocumentType, JsonDocument> schemas = LoadSchemas(issues);
        var identifiers = new Dictionary<string, string>(StringComparer.Ordinal);
        int documentsExamined = 0;

        try
        {
            foreach (string file in files.Where(file => Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Relative(file);
                if (ContentFileClassifier.IsIgnoredJson(relativePath))
                {
                    continue;
                }

                ContentDocumentType? documentType = ContentFileClassifier.Classify(relativePath);
                if (documentType is null)
                {
                    documentsExamined++;
                    AddIssue(issues, "unknown-manifest", relativePath, "$", "Manifest JSON non reconnu par les conventions v1.");
                    continue;
                }

                documentsExamined++;
                if (!schemas.TryGetValue(documentType.Value, out JsonDocument? schema))
                {
                    continue;
                }

                JsonDocument? document = ReadJsonDocument(file, relativePath, issues);
                if (document is null)
                {
                    continue;
                }

                using (document)
                {
                    DetectDuplicateProperties(document.RootElement, relativePath, "$", issues);
                    JsonSchemaSubsetValidator.Validate(
                        document.RootElement,
                        schema.RootElement,
                        relativePath,
                        "$",
                        issues);
                    ValidateIdentifierAndLocation(document.RootElement, documentType.Value, file, relativePath, identifiers, issues);
                    ValidateSemanticRules(document.RootElement, documentType.Value, file, relativePath, issues);
                }
            }
        }
        finally
        {
            foreach (JsonDocument schema in schemas.Values)
            {
                schema.Dispose();
            }
        }

        if (documentsExamined == 0)
        {
            AddIssue(issues, "no-documents", Relative(targetPath), "$", "Aucun manifeste de contenu v1 trouvé.");
        }

        return Task.FromResult(new ContentValidationReport(files.Count, documentsExamined, issues));
    }

    private Dictionary<ContentDocumentType, JsonDocument> LoadSchemas(List<ContentValidationIssue> issues)
    {
        var schemas = new Dictionary<ContentDocumentType, JsonDocument>();
        foreach (ContentDocumentType documentType in Enum.GetValues<ContentDocumentType>())
        {
            string schemaPath = Path.Combine(_schemaRootPath, ContentFileClassifier.SchemaFileName(documentType));
            string relativePath = Relative(schemaPath);
            JsonDocument? schema = ReadJsonDocument(schemaPath, relativePath, issues, "schema");
            if (schema is not null)
            {
                int issueCountBeforeSchemaAudit = issues.Count;
                DetectDuplicateProperties(schema.RootElement, relativePath, "$", issues);
                ValidateSchemaVocabulary(schema.RootElement, relativePath, "$", issues);
                if (issues.Count == issueCountBeforeSchemaAudit)
                {
                    schemas.Add(documentType, schema);
                }
                else
                {
                    schema.Dispose();
                }
            }
        }

        return schemas;
    }

    private JsonDocument? ReadJsonDocument(
        string path,
        string relativePath,
        ICollection<ContentValidationIssue> issues,
        string errorCode = "json")
    {
        if (!File.Exists(path))
        {
            AddIssue(issues, $"{errorCode}-not-found", relativePath, "$", "Fichier JSON introuvable.");
            return null;
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > _maximumFileSizeBytes)
        {
            AddIssue(issues, "file-too-large", relativePath, "$", $"Fichier supérieur à la limite de {_maximumFileSizeBytes} octets.");
            return null;
        }

        try
        {
            string json = StrictUtf8.GetString(File.ReadAllBytes(path));
            return JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64,
            });
        }
        catch (DecoderFallbackException)
        {
            AddIssue(issues, "invalid-utf8", relativePath, "$", "Le fichier doit être encodé en UTF-8 valide.");
        }
        catch (JsonException exception)
        {
            string location = exception.LineNumber is null
                ? "$"
                : $"ligne {exception.LineNumber + 1}, octet {exception.BytePositionInLine + 1}";
            AddIssue(issues, errorCode, relativePath, location, "JSON invalide.");
        }
        catch (IOException)
        {
            AddIssue(issues, "file-read", relativePath, "$", "Lecture du fichier impossible.");
        }
        catch (UnauthorizedAccessException)
        {
            AddIssue(issues, "file-read", relativePath, "$", "Lecture du fichier refusée.");
        }

        return null;
    }

    private static void ValidateIdentifierAndLocation(
        JsonElement root,
        ContentDocumentType documentType,
        string fullPath,
        string relativePath,
        Dictionary<string, string> identifiers,
        ICollection<ContentValidationIssue> issues)
    {
        if (!root.TryGetProperty("id", out JsonElement idElement)
            || idElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

        string identifier = idElement.GetString()!;
        if (identifiers.TryGetValue(identifier, out string? firstFile))
        {
            AddIssue(issues, "duplicate-id", relativePath, "$.id", $"Identifiant déjà déclaré dans {firstFile}.");
        }
        else
        {
            identifiers.Add(identifier, relativePath);
        }

        string expectedLocationName = documentType is ContentDocumentType.Lesson
            or ContentDocumentType.Exercise
            or ContentDocumentType.DebugScenario
            or ContentDocumentType.SqlScenario
            ? Directory.GetParent(fullPath)?.Name ?? string.Empty
            : Path.GetFileNameWithoutExtension(fullPath);

        if (!identifier.Equals(expectedLocationName, StringComparison.Ordinal))
        {
            AddIssue(
                issues,
                "id-path-mismatch",
                relativePath,
                "$.id",
                $"L'identifiant doit correspondre au nom canonique '{expectedLocationName}' porté par le chemin.");
        }
    }

    private void ValidateSemanticRules(
        JsonElement root,
        ContentDocumentType documentType,
        string manifestPath,
        string relativePath,
        ICollection<ContentValidationIssue> issues)
    {
        ValidateReferencedPaths(root, manifestPath, relativePath, "$", issues);

        if (documentType == ContentDocumentType.Lesson
            && root.TryGetProperty("skills", out JsonElement skills))
        {
            ValidateWeightSum(skills, "weight", relativePath, "$.skills", issues);
        }

        if (documentType == ContentDocumentType.Project
            && root.TryGetProperty("rubric", out JsonElement rubric))
        {
            ValidateWeightSum(rubric, "weight", relativePath, "$.rubric", issues);
        }
    }

    private void ValidateReferencedPaths(
        JsonElement element,
        string manifestPath,
        string relativePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string childPath = $"{propertyPath}.{property.Name}";
                if (property.Value.ValueKind == JsonValueKind.String
                    && (property.Name.EndsWith("Path", StringComparison.Ordinal)
                        || property.Name.Equals("path", StringComparison.Ordinal)
                        || property.Name.Equals("statement", StringComparison.Ordinal)
                        || property.Name.Equals("explanation", StringComparison.Ordinal)))
                {
                    ValidateReferencedPath(
                        property.Value.GetString()!,
                        manifestPath,
                        relativePath,
                        childPath,
                        issues);
                }
                else
                {
                    ValidateReferencedPaths(property.Value, manifestPath, relativePath, childPath, issues);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                ValidateReferencedPaths(item, manifestPath, relativePath, $"{propertyPath}[{index}]", issues);
                index++;
            }
        }
    }

    private void ValidateReferencedPath(
        string configuredPath,
        string manifestPath,
        string relativePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)
            || Path.IsPathRooted(configuredPath)
            || configuredPath.Contains(':', StringComparison.Ordinal)
            || Uri.TryCreate(configuredPath, UriKind.Absolute, out _))
        {
            AddIssue(issues, "unsafe-path", relativePath, propertyPath, "Le chemin doit être relatif, local et non vide.");
            return;
        }

        string[] pathSegments = configuredPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Any(segment => segment is "." or ".."))
        {
            AddIssue(issues, "path-traversal", relativePath, propertyPath, "Les segments '.' et '..' sont interdits dans un chemin de contenu.");
            return;
        }

        string platformPath = configuredPath.Replace('/', Path.DirectorySeparatorChar);
        string resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestPath)!, platformPath));
        if (!IsWithin(_contentRootPath, resolvedPath))
        {
            AddIssue(issues, "path-traversal", relativePath, propertyPath, "Le chemin résolu sort de content/.");
            return;
        }

        if (ContainsReparsePointBetween(_contentRootPath, resolvedPath))
        {
            AddIssue(issues, "reparse-point", relativePath, propertyPath, "Lien symbolique ou point de réanalyse interdit.");
            return;
        }

        bool expectsDirectory = configuredPath.EndsWith('/');
        bool exists = expectsDirectory ? Directory.Exists(resolvedPath) : File.Exists(resolvedPath);
        if (!exists)
        {
            AddIssue(issues, "path-not-found", relativePath, propertyPath, "Chemin référencé introuvable ou de type incorrect.");
            return;
        }

        if (!expectsDirectory && Path.GetExtension(resolvedPath).Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            ValidateMarkdown(resolvedPath, relativePath, propertyPath, issues);
        }
    }

    private void ValidateMarkdown(
        string markdownPath,
        string manifestRelativePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        var fileInfo = new FileInfo(markdownPath);
        if (fileInfo.Length == 0)
        {
            AddIssue(issues, "empty-markdown", manifestRelativePath, propertyPath, "Le Markdown référencé est vide.");
            return;
        }

        if (fileInfo.Length > _maximumFileSizeBytes)
        {
            AddIssue(issues, "file-too-large", manifestRelativePath, propertyPath, $"Markdown supérieur à la limite de {_maximumFileSizeBytes} octets.");
            return;
        }

        try
        {
            string markdown = StrictUtf8.GetString(File.ReadAllBytes(markdownPath));
            if (RawHtmlTagRegex().IsMatch(markdown))
            {
                AddIssue(issues, "raw-html", manifestRelativePath, propertyPath, "HTML brut interdit dans le Markdown.");
            }
        }
        catch (DecoderFallbackException)
        {
            AddIssue(issues, "invalid-utf8", manifestRelativePath, propertyPath, "Le Markdown doit être encodé en UTF-8 valide.");
        }
        catch (IOException)
        {
            AddIssue(issues, "file-read", manifestRelativePath, propertyPath, "Lecture du Markdown impossible.");
        }
        catch (UnauthorizedAccessException)
        {
            AddIssue(issues, "file-read", manifestRelativePath, propertyPath, "Lecture du Markdown refusée.");
        }
    }

    private static void ValidateWeightSum(
        JsonElement items,
        string weightProperty,
        string relativePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (items.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        decimal sum = 0;
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty(weightProperty, out JsonElement weight)
                && weight.TryGetDecimal(out decimal value))
            {
                sum += value;
            }
        }

        if (sum != 1m)
        {
            AddIssue(issues, "weight-sum", relativePath, propertyPath, "La somme des pondérations doit être exactement égale à 1.");
        }
    }

    private List<string> EnumerateFilesSafely(
        string root,
        ICollection<ContentValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var files = new List<string>();
        var directories = new Stack<string>();
        directories.Push(root);

        while (directories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string directory = directories.Pop();
            try
            {
                foreach (string entry in Directory.EnumerateFileSystemEntries(directory).OrderBy(path => path, PathComparer))
                {
                    FileAttributes attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        AddIssue(issues, "reparse-point", Relative(entry), "$", "Lien symbolique ou point de réanalyse interdit.");
                        continue;
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        directories.Push(entry);
                    }
                    else
                    {
                        files.Add(entry);
                        if (files.Count > _maximumFiles)
                        {
                            return files;
                        }
                    }
                }
            }
            catch (IOException)
            {
                AddIssue(issues, "directory-read", Relative(directory), "$", "Lecture du dossier impossible.");
            }
            catch (UnauthorizedAccessException)
            {
                AddIssue(issues, "directory-read", Relative(directory), "$", "Lecture du dossier refusée.");
            }
        }

        files.Sort(PathComparer);
        return files;
    }

    private static void DetectDuplicateProperties(
        JsonElement element,
        string relativePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string childPath = $"{propertyPath}.{property.Name}";
                if (!names.Add(property.Name))
                {
                    AddIssue(issues, "duplicate-property", relativePath, childPath, "Propriété JSON dupliquée interdite.");
                }

                DetectDuplicateProperties(property.Value, relativePath, childPath, issues);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                DetectDuplicateProperties(item, relativePath, $"{propertyPath}[{index}]", issues);
                index++;
            }
        }
    }

    private static void ValidateSchemaVocabulary(
        JsonElement schema,
        string relativePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            AddIssue(issues, "schema-shape", relativePath, propertyPath, "Un nœud de schéma doit être un objet JSON.");
            return;
        }

        var supportedKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "$schema",
            "$id",
            "title",
            "description",
            "type",
            "additionalProperties",
            "required",
            "properties",
            "const",
            "enum",
            "pattern",
            "minLength",
            "maxLength",
            "minimum",
            "exclusiveMinimum",
            "maximum",
            "minItems",
            "maxItems",
            "uniqueItems",
            "items",
            "prefixItems",
        };

        foreach (JsonProperty keyword in schema.EnumerateObject())
        {
            if (!supportedKeywords.Contains(keyword.Name))
            {
                AddIssue(
                    issues,
                    "unsupported-schema-keyword",
                    relativePath,
                    $"{propertyPath}.{keyword.Name}",
                    "Mot-clé JSON Schema non pris en charge par le validateur v1.");
                continue;
            }

            if (keyword.Name == "properties" && keyword.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty propertySchema in keyword.Value.EnumerateObject())
                {
                    ValidateSchemaVocabulary(
                        propertySchema.Value,
                        relativePath,
                        $"{propertyPath}.properties.{propertySchema.Name}",
                        issues);
                }
            }
            else if (keyword.Name == "items" && keyword.Value.ValueKind == JsonValueKind.Object)
            {
                ValidateSchemaVocabulary(keyword.Value, relativePath, $"{propertyPath}.items", issues);
            }
            else if (keyword.Name == "prefixItems" && keyword.Value.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement prefixSchema in keyword.Value.EnumerateArray())
                {
                    ValidateSchemaVocabulary(prefixSchema, relativePath, $"{propertyPath}.prefixItems[{index}]", issues);
                    index++;
                }
            }
        }
    }

    private static bool ContainsReparsePointBetween(string root, string candidate)
    {
        if (!IsWithin(root, candidate))
        {
            return true;
        }

        string relative = Path.GetRelativePath(root, candidate);
        string current = root;
        foreach (string segment in relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current))
            {
                continue;
            }

            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private string Relative(string path)
    {
        string relative = Path.GetRelativePath(_contentRootPath, path).Replace('\\', '/');
        return relative == "." ? "." : relative;
    }

    private static string NormalizeDirectory(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool IsWithin(string root, string candidate)
    {
        string normalizedRoot = NormalizeDirectory(root);
        string normalizedCandidate = Path.GetFullPath(candidate);
        return normalizedCandidate.Equals(normalizedRoot, PathComparison)
            || normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, PathComparison);
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static void AddIssue(
        ICollection<ContentValidationIssue> issues,
        string code,
        string filePath,
        string propertyPath,
        string message) => issues.Add(new ContentValidationIssue(code, filePath, propertyPath, message));

    [GeneratedRegex(@"<\s*/?\s*[A-Za-z][^>]*>", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex RawHtmlTagRegex();
}
