using System.Collections;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.UnitTests;

public sealed class ContentCatalogTests
{
    [Fact]
    public void IndexesAndSearchAreDeterministicAndAccentInsensitive()
    {
        var lesson = CreateItem(
            "lesson-evaluate",
            ContentDocumentType.Lesson,
            "Évaluer un type",
            "Justifier une décision monétaire",
            ["csharp.types"]);
        var english = new ContentCatalogItem(
            "english-edge-case",
            1,
            ContentDocumentType.EnglishActivity,
            "Clarifier un ticket",
            "Poser une question précise",
            ["english.clarification"],
            [],
            ["edge case cas limite"]);
        var catalog = new ContentCatalog("revision-a", [english, lesson]);

        Assert.Same(lesson, catalog.FindById("lesson-evaluate"));
        Assert.Equal([lesson], catalog.GetByType(ContentDocumentType.Lesson));
        Assert.Equal([lesson], catalog.GetBySkill("csharp.types"));
        Assert.Equal([lesson], catalog.Search("EVALUER MONETAIRE"));
        Assert.Equal([english], catalog.Search("EDGE case"));
        Assert.Equal([english, lesson], catalog.Search(string.Empty));
    }

    [Fact]
    public void SnapshotCollectionsCannotBeMutatedByCallers()
    {
        var catalog = new ContentCatalog(
            "revision-a",
            [CreateItem("lesson-a", ContentDocumentType.Lesson, "Leçon A", "Résumé A", ["skill.a"])]);

        var items = Assert.IsAssignableFrom<IList>(catalog.Items);
        Assert.Throws<NotSupportedException>(() => items[0] = CreateItem(
            "lesson-b",
            ContentDocumentType.Lesson,
            "Leçon B",
            "Résumé B",
            ["skill.b"]));

        var skills = Assert.IsAssignableFrom<IList>(catalog.Items[0].Skills);
        Assert.Throws<NotSupportedException>(() => skills[0] = "skill.changed");
    }

    [Fact]
    public void DuplicateIdentifiersAreRejectedByTheDomainSnapshot()
    {
        ContentCatalogItem first = CreateItem(
            "duplicate",
            ContentDocumentType.Lesson,
            "Premier",
            "Premier résumé",
            []);
        ContentCatalogItem second = CreateItem(
            "duplicate",
            ContentDocumentType.Exercise,
            "Second",
            "Second résumé",
            []);

        Assert.Throws<ArgumentException>(() => new ContentCatalog("revision", [first, second]));
    }

    private static ContentCatalogItem CreateItem(
        string id,
        ContentDocumentType type,
        string title,
        string summary,
        IEnumerable<string> skills) => new(id, 1, type, title, summary, skills, []);
}
