using System.Reflection;
using ForgeDotNet.Application.Content;
using ForgeDotNet.Domain.Content;
using ForgeDotNet.Infrastructure.Content;

namespace ForgeDotNet.IntegrationTests;

public sealed class ContentCatalogLoadingTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ContentRoot = Path.Combine(RepositoryRoot, "content");
    private static readonly string ReferenceRoot = Path.Combine(ContentRoot, "reference");

    [Fact]
    public async Task ReferenceContentLoadsWithStableIndexesAndSearch()
    {
        FileSystemContentCatalogLoader loader = CreateLoader();

        ContentCatalogLoadResult first = await loader.LoadAsync(ReferenceRoot);
        ContentCatalogLoadResult second = await loader.LoadAsync(ReferenceRoot);

        Assert.True(first.Succeeded, FormatIssues(first.Issues));
        Assert.True(second.Succeeded, FormatIssues(second.Issues));
        ContentCatalog catalog = Assert.IsType<ContentCatalog>(first.Catalog);
        ContentCatalog secondCatalog = Assert.IsType<ContentCatalog>(second.Catalog);
        Assert.Equal(231, catalog.Items.Count);
        Assert.Equal(85, catalog.GetByType(ContentDocumentType.Exercise).Count);
        Assert.Equal(84, catalog.GetByType(ContentDocumentType.InterviewQuestion).Count);
        Assert.Single(catalog.GetByType(ContentDocumentType.Curriculum));
        Assert.Equal(30, catalog.GetByType(ContentDocumentType.Lesson).Count);
        Assert.Single(catalog.GetByType(ContentDocumentType.EnglishActivity));
        Assert.Equal(25, catalog.GetByType(ContentDocumentType.DebugScenario).Count);
        Assert.Equal(5, catalog.GetByType(ContentDocumentType.Project).Count);
        Assert.Equal(catalog.Revision, secondCatalog.Revision);
        string[] csharpTypeIds = catalog.GetBySkill("csharp.types")
            .Select(item => item.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            csharpTypeIds,
            secondCatalog.GetBySkill("csharp.types")
                .Select(item => item.Id)
                .OrderBy(id => id, StringComparer.Ordinal));
        Assert.Contains("reference-types-001", csharpTypeIds);
        Assert.Contains("reference-total-001", csharpTypeIds);
        Assert.Equal(["reference-types-001"], catalog.Search("EVALUER monetaire").Select(item => item.Id));
        Assert.Equal(["reference-glossary-001"], catalog.Search("EDGE CASE").Select(item => item.Id));
    }

    [Fact]
    public async Task MissingReferenceIsRejectedWithFileAndProperty()
    {
        string fixtureRoot = CopyReferenceContent();
        string exercisePath = Path.Combine(fixtureRoot, "exercises", "reference-total-001", "exercise.json");
        ReplaceInFile(exercisePath, "reference-types-001", "missing-content-001");

        try
        {
            ContentCatalogLoadResult result = await CreateLoader().LoadAsync(fixtureRoot);

            ContentValidationIssue issue = Assert.Single(result.Issues, issue => issue.Code == "missing-reference");
            Assert.EndsWith("exercise.json", issue.FilePath, StringComparison.Ordinal);
            Assert.Equal("$.prerequisites[0]", issue.PropertyPath);
            Assert.False(result.Succeeded);
        }
        finally
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DirectAndIndirectPrerequisiteCyclesAreRejected()
    {
        string directRoot = CopyReferenceContent();
        string indirectRoot = CopyReferenceContent();
        string directLesson = Path.Combine(directRoot, "curriculum", "lessons", "reference-types-001", "lesson.json");
        string indirectLesson = Path.Combine(indirectRoot, "curriculum", "lessons", "reference-types-001", "lesson.json");
        ReplaceInFile(directLesson, "\"prerequisites\": []", "\"prerequisites\": [\"reference-types-001\"]");
        ReplaceInFile(indirectLesson, "\"prerequisites\": []", "\"prerequisites\": [\"reference-total-001\"]");

        try
        {
            ContentCatalogLoadResult direct = await CreateLoader().LoadAsync(directRoot);
            ContentCatalogLoadResult indirect = await CreateLoader().LoadAsync(indirectRoot);

            Assert.Contains(direct.Issues, issue => issue.Code == "dependency-cycle");
            Assert.Contains(indirect.Issues, issue =>
                issue.Code == "dependency-cycle"
                && issue.Message.Contains("reference-types-001", StringComparison.Ordinal)
                && issue.Message.Contains("reference-total-001", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directRoot, recursive: true);
            Directory.Delete(indirectRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DuplicateIdAndWrongReferenceTypeAreRejected()
    {
        string duplicateRoot = CopyReferenceContent();
        string wrongTypeRoot = CopyReferenceContent();
        string englishSource = Path.Combine(duplicateRoot, "english", "reference-glossary-001.json");
        string duplicatePath = Path.Combine(duplicateRoot, "english", "reference-interview-001.json");
        string duplicatedContent = File.ReadAllText(englishSource)
            .Replace("reference-glossary-001", "reference-interview-001", StringComparison.Ordinal);
        File.WriteAllText(duplicatePath, duplicatedContent);
        string exercisePath = Path.Combine(wrongTypeRoot, "exercises", "reference-total-001", "exercise.json");
        ReplaceInFile(exercisePath, "reference-interview-001", "reference-types-001");

        try
        {
            ContentCatalogLoadResult duplicate = await CreateLoader().LoadAsync(duplicateRoot);
            ContentCatalogLoadResult wrongType = await CreateLoader().LoadAsync(wrongTypeRoot);

            Assert.Contains(duplicate.Issues, issue => issue.Code == "duplicate-id");
            Assert.Contains(wrongType.Issues, issue =>
                issue.Code == "reference-type" && issue.PropertyPath == "$.interviewQuestionId");
        }
        finally
        {
            Directory.Delete(duplicateRoot, recursive: true);
            Directory.Delete(wrongTypeRoot, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidReloadPreservesPublishedSnapshotAndSensitiveFieldsStayExcluded()
    {
        string invalidRoot = CopyReferenceContent();
        string exercisePath = Path.Combine(invalidRoot, "exercises", "reference-total-001", "exercise.json");
        ReplaceInFile(exercisePath, "reference-types-001", "missing-content-001");
        using var provider = new ContentCatalogProvider(CreateLoader());

        try
        {
            ContentCatalogReloadResult initial = await provider.ReloadAsync(ReferenceRoot);
            ContentCatalog published = provider.Current;
            ContentCatalogReloadResult failed = await provider.ReloadAsync(invalidRoot);

            Assert.True(initial.Succeeded);
            Assert.False(failed.Succeeded);
            Assert.True(failed.PreviousSnapshotPreserved);
            Assert.Same(published, provider.Current);
            Assert.Empty(published.Search("réponse modèle sensible"));
            string[] publicProperties = typeof(ContentCatalogItem)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .ToArray();
            Assert.DoesNotContain(publicProperties, name =>
                name.Contains("Solution", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Hidden", StringComparison.OrdinalIgnoreCase)
                || name.Contains("ModelAnswer", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(invalidRoot, recursive: true);
        }
    }

    private static FileSystemContentCatalogLoader CreateLoader()
    {
        var options = new ContentValidationOptions { ContentRootPath = ContentRoot };
        var validator = new FileSystemContentValidationService(options);
        return new FileSystemContentCatalogLoader(validator, options);
    }

    private static string CopyReferenceContent()
    {
        string destination = Path.Combine(ContentRoot, $".catalog-test-{Guid.NewGuid():N}");
        CopyDirectory(ReferenceRoot, destination);
        return destination;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }
    }

    private static void ReplaceInFile(string path, string oldValue, string newValue)
    {
        string before = File.ReadAllText(path);
        string after = before.Replace(oldValue, newValue, StringComparison.Ordinal);
        Assert.NotEqual(before, after);
        File.WriteAllText(path, after);
    }

    private static string FormatIssues(IEnumerable<ContentValidationIssue> issues) =>
        string.Join(Environment.NewLine, issues.Select(issue =>
            $"{issue.FilePath} | {issue.PropertyPath} | {issue.Code} | {issue.Message}"));

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
