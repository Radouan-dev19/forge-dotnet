using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

/// <summary>
/// Refuse le contenu structurellement valide mais pédagogiquement creux : marqueurs de
/// génération non substitués, paragraphes recopiés d'un document à l'autre et leçons dont
/// l'explication ne fait que répéter l'intuition sans jamais montrer de code.
/// </summary>
/// <remarks>
/// Ces règles existent parce que les schémas et les tests d'exécution acceptaient
/// soixante-dix leçons clonées : ils contrôlaient la structure, jamais l'authenticité.
/// </remarks>
internal sealed partial class ContentAuthenticityAnalyzer
{
    private const string PlaceholderCode = ContentAuthenticityRules.Placeholder;
    private const string ClonedContentCode = ContentAuthenticityRules.ClonedContent;
    private const string HollowLessonCode = ContentAuthenticityRules.HollowLesson;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly string _contentRootPath;
    private readonly long _maximumFileSizeBytes;
    private readonly int _maximumCloneOccurrences;
    private readonly int _minimumCloneParagraphWords;

    public ContentAuthenticityAnalyzer(ContentValidationOptions options, string contentRootPath)
    {
        _contentRootPath = contentRootPath;
        _maximumFileSizeBytes = options.MaximumFileSizeBytes;
        _maximumCloneOccurrences = Math.Max(1, options.MaximumCloneOccurrences);
        _minimumCloneParagraphWords = Math.Max(1, options.MinimumCloneParagraphWords);
    }

    public void Analyze(
        IReadOnlyList<ContentManifestReference> manifests,
        ICollection<ContentValidationIssue> issues)
    {
        // Paragraphe normalisé -> documents qui le portent.
        var paragraphOwners = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (ContentManifestReference manifest in manifests)
        {
            AnalyzeManifest(manifest, paragraphOwners, issues);
        }

        ReportClones(paragraphOwners, issues);
    }

    private void AnalyzeManifest(
        ContentManifestReference manifest,
        Dictionary<string, SortedSet<string>> paragraphOwners,
        ICollection<ContentValidationIssue> issues)
    {
        JsonDocument? document = TryReadJson(manifest.FullPath);
        if (document is null)
        {
            return;
        }

        using (document)
        {
            var strings = new List<(string PropertyPath, string Value)>();
            CollectStrings(document.RootElement, "$", strings);

            string? placeholderLocation = null;
            string? placeholderMarker = null;

            foreach ((string propertyPath, string value) in strings)
            {
                Match match = PlaceholderRegex().Match(value);
                if (match.Success && placeholderMarker is null)
                {
                    placeholderMarker = match.Value;
                    placeholderLocation = propertyPath;
                }

                AccumulateParagraphs(value, manifest.RelativePath, paragraphOwners);
            }

            foreach (string markdownPath in ResolveMarkdownPaths(manifest, strings))
            {
                string? markdown = TryReadText(markdownPath);
                if (markdown is null)
                {
                    continue;
                }

                string prose = MarkdownProse.Extract(markdown);
                Match match = PlaceholderRegex().Match(prose);
                if (match.Success && placeholderMarker is null)
                {
                    placeholderMarker = match.Value;
                    placeholderLocation = Relative(markdownPath);
                }

                AccumulateParagraphs(prose, manifest.RelativePath, paragraphOwners);

                if (manifest.Type == ContentDocumentType.Lesson)
                {
                    ValidateLessonSubstance(manifest, markdown, Relative(markdownPath), issues);
                }
            }

            if (placeholderMarker is not null)
            {
                issues.Add(new ContentValidationIssue(
                    PlaceholderCode,
                    manifest.RelativePath,
                    placeholderLocation ?? "$",
                    $"Marqueur de génération non substitué détecté : « {Truncate(placeholderMarker)} ». "
                    + "Le contenu publié ne doit contenir aucun gabarit à compléter."));
            }
        }
    }

    private static void ValidateLessonSubstance(
        ContentManifestReference manifest,
        string markdown,
        string markdownRelativePath,
        ICollection<ContentValidationIssue> issues)
    {
        string intuition = Normalize(ExtractSection(markdown, "Intuition"));
        string explanation = Normalize(ExtractSection(markdown, "Explication"));

        if (intuition.Length > 0
            && explanation.Length > 0
            && explanation.Contains(intuition, StringComparison.Ordinal))
        {
            issues.Add(new ContentValidationIssue(
                HollowLessonCode,
                manifest.RelativePath,
                markdownRelativePath,
                "La section « Explication » recopie intégralement la section « Intuition » : "
                + "elle doit développer la notion, pas la répéter."));
        }

        if (!MarkdownProse.ContainsCodeFence(markdown))
        {
            issues.Add(new ContentValidationIssue(
                HollowLessonCode,
                manifest.RelativePath,
                markdownRelativePath,
                "La leçon ne contient aucun bloc de code clôturé : une notion technique doit être "
                + "montrée, pas seulement décrite."));
        }
    }

    private void ReportClones(
        Dictionary<string, SortedSet<string>> paragraphOwners,
        ICollection<ContentValidationIssue> issues)
    {
        var clonesByOwner = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach ((_, SortedSet<string> owners) in paragraphOwners)
        {
            if (owners.Count <= _maximumCloneOccurrences)
            {
                continue;
            }

            foreach (string owner in owners)
            {
                clonesByOwner[owner] = clonesByOwner.GetValueOrDefault(owner) + 1;
            }
        }

        foreach ((string owner, int count) in clonesByOwner.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            issues.Add(new ContentValidationIssue(
                ClonedContentCode,
                owner,
                "$",
                $"{count} paragraphe(s) de ce document sont recopiés dans plus de "
                + $"{_maximumCloneOccurrences} documents du lot. Un contenu recopié n'enseigne pas "
                + "la notion annoncée."));
        }
    }

    private void AccumulateParagraphs(
        string text,
        string ownerRelativePath,
        Dictionary<string, SortedSet<string>> paragraphOwners)
    {
        foreach (string paragraph in SplitParagraphs(text))
        {
            string normalized = Normalize(paragraph);
            if (CountWords(normalized) < _minimumCloneParagraphWords)
            {
                continue;
            }

            if (!paragraphOwners.TryGetValue(normalized, out SortedSet<string>? owners))
            {
                owners = new SortedSet<string>(StringComparer.Ordinal);
                paragraphOwners.Add(normalized, owners);
            }

            owners.Add(ownerRelativePath);
        }
    }

    private static IEnumerable<string> SplitParagraphs(string text)
    {
        var buffer = new StringBuilder();
        foreach (string rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                continue;
            }

            if (buffer.Length > 0)
            {
                buffer.Append(' ');
            }

            buffer.Append(line);
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }

    private IEnumerable<string> ResolveMarkdownPaths(
        ContentManifestReference manifest,
        IEnumerable<(string PropertyPath, string Value)> strings)
    {
        string manifestDirectory = Path.GetDirectoryName(manifest.FullPath)!;
        var seen = new HashSet<string>(PathComparer);
        foreach ((_, string value) in strings)
        {
            if (!value.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || value.Length > 240
                || Path.IsPathRooted(value)
                || value.Contains("..", StringComparison.Ordinal))
            {
                continue;
            }

            string resolved = Path.GetFullPath(Path.Combine(
                manifestDirectory,
                value.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsWithinContentRoot(resolved) || !File.Exists(resolved) || !seen.Add(resolved))
            {
                continue;
            }

            yield return resolved;
        }
    }

    private static void CollectStrings(
        JsonElement element,
        string propertyPath,
        ICollection<(string PropertyPath, string Value)> strings)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    CollectStrings(property.Value, $"{propertyPath}.{property.Name}", strings);
                }

                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    CollectStrings(item, $"{propertyPath}[{index}]", strings);
                    index++;
                }

                break;
            case JsonValueKind.String:
                strings.Add((propertyPath, element.GetString() ?? string.Empty));
                break;
            default:
                break;
        }
    }

    private static string ExtractSection(string markdown, string title)
    {
        string[] lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var body = new StringBuilder();
        bool inside = false;
        foreach (string line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (inside)
                {
                    break;
                }

                inside = string.Equals(line[3..].Trim(), title, StringComparison.Ordinal);
                continue;
            }

            if (inside)
            {
                body.AppendLine(line);
            }
        }

        return body.ToString();
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        bool pendingSpace = false;
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLower(character, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static int CountWords(string normalized) =>
        normalized.Length == 0 ? 0 : normalized.Count(character => character == ' ') + 1;

    private static string Truncate(string value) =>
        value.Length <= 60 ? value : value[..60] + "…";

    private string? TryReadText(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            // Les fins de ligne sont normalisées une fois pour toutes : sans cela, un retour
            // chariot laissé en fin de clôture ferait échouer la détection des blocs de code.
            return fileInfo.Length == 0 || fileInfo.Length > _maximumFileSizeBytes
                ? null
                : StrictUtf8.GetString(File.ReadAllBytes(path))
                    .Replace("\r\n", "\n", StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or DecoderFallbackException)
        {
            // Le contrôle principal a déjà signalé le fichier illisible : ne pas le doubler.
            return null;
        }
    }

    private JsonDocument? TryReadJson(string path)
    {
        string? json = TryReadText(path);
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64,
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private bool IsWithinContentRoot(string candidate) =>
        candidate.Equals(_contentRootPath, PathComparison)
        || candidate.StartsWith(_contentRootPath + Path.DirectorySeparatorChar, PathComparison);

    private string Relative(string path) =>
        Path.GetRelativePath(_contentRootPath, path).Replace('\\', '/');

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    [GeneratedRegex(
        @"\$\(|\$\{|\$[A-Za-z_][A-Za-z0-9_]*|\{\{[^{}]*\}\}|\bTODO\b|\bFIXME\b|À\s+COMPLÉTER|<placeholder>",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        1000)]
    private static partial Regex PlaceholderRegex();

}

internal readonly record struct ContentManifestReference(
    string RelativePath,
    string FullPath,
    ContentDocumentType Type);
