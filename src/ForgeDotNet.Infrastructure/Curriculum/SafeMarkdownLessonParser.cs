using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ForgeDotNet.Application.Curriculum;

namespace ForgeDotNet.Infrastructure.Curriculum;

public sealed partial class SafeMarkdownLessonParser
{
    private static readonly (string Id, string Title)[] ExpectedSections =
    [
        ("objectif", "Objectif observable"),
        ("prerequis", "Prérequis"),
        ("intuition", "Intuition"),
        ("explication", "Explication"),
        ("exemple", "Exemple commenté"),
        ("contre-exemple", "Contre-exemple et erreur fréquente"),
        ("comprehension", "Vérification de compréhension"),
        ("guide", "Exercice guidé"),
        ("autonome", "Exercice autonome"),
        ("debogage", "Débogage"),
        ("entretien", "Entretien"),
        ("resume", "Résumé"),
        ("cartes", "Cartes de révision"),
        ("maitrise", "Test de maîtrise"),
    ];

    public static LessonParsedMarkdown Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        string[] lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var sections = new List<LessonSectionView>();
        List<string>? sectionLines = null;
        string? sectionTitle = null;
        LessonQuizDefinition? quiz = null;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AddSection();
                sectionTitle = line[3..].Trim();
                sectionLines = [];
                continue;
            }

            if (sectionLines is null)
            {
                continue;
            }

            if (line.Equals(":::quiz", StringComparison.Ordinal))
            {
                var quizLines = new List<string>();
                bool closed = false;
                while (++index < lines.Length)
                {
                    if (lines[index].Equals(":::", StringComparison.Ordinal))
                    {
                        closed = true;
                        break;
                    }

                    quizLines.Add(lines[index]);
                }

                if (!closed || quiz is not null)
                {
                    throw new InvalidDataException("Le bloc quiz de la leçon est invalide ou dupliqué.");
                }

                quiz = ParseQuiz(quizLines);
                continue;
            }

            sectionLines.Add(line);
        }

        AddSection();
        ValidateSections(sections);
        return new LessonParsedMarkdown(
            Array.AsReadOnly(sections.ToArray()),
            quiz ?? throw new InvalidDataException("La leçon ne contient aucun quiz de compréhension structuré."));

        void AddSection()
        {
            if (sectionLines is null || sectionTitle is null)
            {
                return;
            }

            string sectionId = ExpectedSections
                .Where(section => string.Equals(section.Title, sectionTitle, StringComparison.Ordinal))
                .Select(section => section.Id)
                .SingleOrDefault()
                ?? Slug(sectionTitle);
            sections.Add(new LessonSectionView(
                sectionId,
                sectionTitle,
                Array.AsReadOnly(ParseBlocks(sectionLines).ToArray())));
        }
    }

    private static void ValidateSections(List<LessonSectionView> sections)
    {
        if (sections.Count != ExpectedSections.Length)
        {
            throw new InvalidDataException(
                $"La leçon doit contenir exactement {ExpectedSections.Length} sections de lecteur.");
        }

        for (int index = 0; index < ExpectedSections.Length; index++)
        {
            if (!string.Equals(sections[index].Id, ExpectedSections[index].Id, StringComparison.Ordinal)
                || !string.Equals(sections[index].Title, ExpectedSections[index].Title, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"La section {index + 1} doit être « {ExpectedSections[index].Title} ».");
            }

            if (sections[index].Blocks.Count == 0)
            {
                throw new InvalidDataException(
                    $"La section « {ExpectedSections[index].Title} » ne peut pas être vide.");
            }
        }
    }

    private static IEnumerable<LessonBlockView> ParseBlocks(List<string> lines)
    {
        for (int index = 0; index < lines.Count;)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                index++;
                continue;
            }

            if (lines[index].StartsWith("```", StringComparison.Ordinal))
            {
                string language = SanitizeLanguage(lines[index][3..].Trim());
                var code = new StringBuilder();
                bool closed = false;
                while (++index < lines.Count)
                {
                    if (lines[index].Equals("```", StringComparison.Ordinal))
                    {
                        closed = true;
                        index++;
                        break;
                    }

                    if (code.Length > 0)
                    {
                        code.AppendLine();
                    }

                    code.Append(lines[index]);
                }

                if (!closed)
                {
                    throw new InvalidDataException("Un bloc de code Markdown n'est pas fermé.");
                }

                yield return new LessonCodeView(code.ToString(), language);
                continue;
            }

            bool ordered = OrderedListRegex().IsMatch(lines[index]);
            if (lines[index].StartsWith("- ", StringComparison.Ordinal) || ordered)
            {
                var items = new List<IReadOnlyList<LessonInlineView>>();
                while (index < lines.Count)
                {
                    Match orderedMatch = OrderedListRegex().Match(lines[index]);
                    bool currentOrdered = orderedMatch.Success;
                    bool currentUnordered = lines[index].StartsWith("- ", StringComparison.Ordinal);
                    if (currentOrdered != ordered || (!currentOrdered && !currentUnordered))
                    {
                        break;
                    }

                    string value = currentOrdered ? orderedMatch.Groups[1].Value : lines[index][2..];
                    items.Add(Array.AsReadOnly(ParseInlines(value).ToArray()));
                    index++;
                }

                yield return new LessonListView(ordered, Array.AsReadOnly(items.ToArray()));
                continue;
            }

            var paragraph = new StringBuilder();
            while (index < lines.Count
                   && !string.IsNullOrWhiteSpace(lines[index])
                   && !lines[index].StartsWith("```", StringComparison.Ordinal)
                   && !lines[index].StartsWith("- ", StringComparison.Ordinal)
                   && !OrderedListRegex().IsMatch(lines[index]))
            {
                if (paragraph.Length > 0)
                {
                    paragraph.Append(' ');
                }

                paragraph.Append(lines[index].TrimStart('#', ' ', '>'));
                index++;
            }

            yield return new LessonParagraphView(
                Array.AsReadOnly(ParseInlines(paragraph.ToString()).ToArray()));
        }
    }

    private static IEnumerable<LessonInlineView> ParseInlines(string text)
    {
        int position = 0;
        foreach (Match match in InlineRegex().Matches(text))
        {
            if (match.Index > position)
            {
                yield return new LessonInlineView(
                    LessonInlineKind.Text,
                    text[position..match.Index]);
            }

            if (match.Groups["label"].Success)
            {
                string label = match.Groups["label"].Value;
                string href = match.Groups["href"].Value;
                yield return IsSafeLink(href)
                    ? new LessonInlineView(LessonInlineKind.Link, label, href)
                    : new LessonInlineView(LessonInlineKind.Text, label);
            }
            else if (match.Groups["strong"].Success)
            {
                yield return new LessonInlineView(LessonInlineKind.Strong, match.Groups["strong"].Value);
            }
            else
            {
                yield return new LessonInlineView(LessonInlineKind.Code, match.Groups["code"].Value);
            }

            position = match.Index + match.Length;
        }

        if (position < text.Length)
        {
            yield return new LessonInlineView(LessonInlineKind.Text, text[position..]);
        }
    }

    private static LessonQuizDefinition ParseQuiz(IReadOnlyList<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var options = new List<string>();
        foreach (string line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new InvalidDataException("Une ligne du quiz n'utilise pas le format clé=valeur.");
            }

            string key = line[..separator];
            string value = line[(separator + 1)..].Trim();
            if (key.Equals("option", StringComparison.Ordinal))
            {
                options.Add(value);
            }
            else if (!values.TryAdd(key, value))
            {
                throw new InvalidDataException($"La clé de quiz « {key} » est dupliquée.");
            }
        }

        string id = Required(values, "id", 80);
        string prompt = Required(values, "question", 500);
        string success = Required(values, "success", 500);
        string retry = Required(values, "retry", 500);
        if (options.Count is < 2 or > 6 || options.Any(option => option.Length is 0 or > 300))
        {
            throw new InvalidDataException("Le quiz doit proposer entre deux et six réponses non vides.");
        }

        if (!int.TryParse(Required(values, "correct", 2), CultureInfo.InvariantCulture, out int correct)
            || correct < 0
            || correct >= options.Count)
        {
            throw new InvalidDataException("L'index de réponse correcte du quiz est invalide.");
        }

        LessonQuizOptionView[] publicOptions = options
            .Select((option, index) => new LessonQuizOptionView(index, option))
            .ToArray();
        var publicView = new LessonQuizView(id, prompt, Array.AsReadOnly(publicOptions));
        return new LessonQuizDefinition(publicView, correct, success, retry);
    }

    private static string Required(
        Dictionary<string, string> values,
        string key,
        int maximumLength)
    {
        if (!values.TryGetValue(key, out string? value)
            || value.Length == 0
            || value.Length > maximumLength)
        {
            throw new InvalidDataException($"La valeur « {key} » du quiz est absente ou trop longue.");
        }

        return value;
    }

    private static bool IsSafeLink(string href)
    {
        if (href.StartsWith('#') || (href.StartsWith('/') && !href.StartsWith("//", StringComparison.Ordinal)))
        {
            return !href.Contains('\\') && !href.Any(char.IsControl);
        }

        return Uri.TryCreate(href, UriKind.Absolute, out Uri? uri)
            && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(uri.UserInfo);
    }

    private static string SanitizeLanguage(string language) =>
        LanguageRegex().IsMatch(language) ? language : string.Empty;

    private static string Slug(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        bool separator = false;
        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                if (separator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                separator = false;
            }
            else
            {
                separator = true;
            }
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"^\d+\.\s+(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(
        @"\[(?<label>[^\]]{1,200})\]\((?<href>[^\s\)]{1,2048})\)|\*\*(?<strong>[^*]{1,500})\*\*|`(?<code>[^`]{1,300})`",
        RegexOptions.CultureInvariant)]
    private static partial Regex InlineRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9+#.-]{0,24}$", RegexOptions.CultureInvariant)]
    private static partial Regex LanguageRegex();
}

public sealed record LessonParsedMarkdown(
    IReadOnlyList<LessonSectionView> Sections,
    LessonQuizDefinition Quiz);
