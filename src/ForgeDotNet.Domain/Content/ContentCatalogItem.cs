using System.Collections.ObjectModel;

namespace ForgeDotNet.Domain.Content;

public sealed class ContentCatalogItem
{
    private readonly string _searchableText;

    public ContentCatalogItem(
        string id,
        int version,
        ContentDocumentType type,
        string title,
        string summary,
        IEnumerable<string> skills,
        IEnumerable<string> prerequisites,
        IEnumerable<string>? glossary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(skills);
        ArgumentNullException.ThrowIfNull(prerequisites);

        Id = id;
        Version = version;
        Type = type;
        Title = title;
        Summary = summary ?? string.Empty;
        Skills = CopyDistinct(skills);
        Prerequisites = CopyDistinct(prerequisites);
        Glossary = CopyDistinct(glossary ?? []);
        _searchableText = ContentSearchText.Normalize(
            string.Join(' ', new[] { Title, Summary }.Concat(Glossary)));
    }

    public string Id { get; }

    public int Version { get; }

    public ContentDocumentType Type { get; }

    public string Title { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Skills { get; }

    public IReadOnlyList<string> Prerequisites { get; }

    public IReadOnlyList<string> Glossary { get; }

    internal string SearchableText => _searchableText;

    private static ReadOnlyCollection<string> CopyDistinct(IEnumerable<string> values) =>
        Array.AsReadOnly(values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray());
}
