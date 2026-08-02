using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace ForgeDotNet.Domain.Content;

public sealed class ContentCatalog
{
    private static readonly ReadOnlyCollection<ContentCatalogItem> NoItems = Array.AsReadOnly<ContentCatalogItem>([]);
    private readonly FrozenDictionary<string, ContentCatalogItem> _byId;
    private readonly FrozenDictionary<string, ReadOnlyCollection<ContentCatalogItem>> _bySkill;
    private readonly FrozenDictionary<ContentDocumentType, ReadOnlyCollection<ContentCatalogItem>> _byType;

    public ContentCatalog(string revision, IEnumerable<ContentCatalogItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        ArgumentNullException.ThrowIfNull(items);

        ContentCatalogItem[] orderedItems = items
            .OrderBy(item => ContentSearchText.Normalize(item.Title), StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        var duplicates = orderedItems
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new ArgumentException($"Identifiants de catalogue dupliqués : {string.Join(", ", duplicates)}.", nameof(items));
        }

        Revision = revision;
        Items = Array.AsReadOnly(orderedItems);
        _byId = orderedItems.ToFrozenDictionary(item => item.Id, StringComparer.Ordinal);
        _byType = orderedItems
            .GroupBy(item => item.Type)
            .ToFrozenDictionary(
                group => group.Key,
                group => Array.AsReadOnly(group.ToArray()));
        _bySkill = orderedItems
            .SelectMany(item => item.Skills.Select(skill => (Skill: skill, Item: item)))
            .GroupBy(entry => entry.Skill, StringComparer.Ordinal)
            .ToFrozenDictionary(
                group => group.Key,
                group => Array.AsReadOnly(group.Select(entry => entry.Item).ToArray()),
                StringComparer.Ordinal);
    }

    public static ContentCatalog Empty { get; } = new("empty", []);

    public string Revision { get; }

    public IReadOnlyList<ContentCatalogItem> Items { get; }

    public ContentCatalogItem? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _byId.GetValueOrDefault(id);
    }

    public IReadOnlyList<ContentCatalogItem> GetByType(ContentDocumentType type) =>
        _byType.GetValueOrDefault(type, NoItems);

    public IReadOnlyList<ContentCatalogItem> GetBySkill(string skill)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skill);
        return _bySkill.GetValueOrDefault(skill, NoItems);
    }

    public IReadOnlyList<ContentCatalogItem> Search(
        string query,
        ContentDocumentType? type = null,
        string? skill = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        string[] terms = ContentSearchText.Normalize(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IEnumerable<ContentCatalogItem> candidates = string.IsNullOrWhiteSpace(skill)
            ? Items
            : GetBySkill(skill);

        if (type is not null)
        {
            candidates = candidates.Where(item => item.Type == type.Value);
        }

        if (terms.Length > 0)
        {
            candidates = candidates.Where(item =>
                terms.All(term => item.SearchableText.Contains(term, StringComparison.Ordinal)));
        }

        return Array.AsReadOnly(candidates.ToArray());
    }
}
