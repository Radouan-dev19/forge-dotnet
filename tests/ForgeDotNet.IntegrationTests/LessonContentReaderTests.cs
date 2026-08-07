using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Curriculum;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Curriculum;

namespace ForgeDotNet.IntegrationTests;

public sealed class LessonContentReaderTests
{
    [Fact]
    public async Task ReferenceLessonContainsFourteenOrderedSectionsAndAUsefulQuiz()
    {
        using ContentCatalogProvider provider = await CreateCatalogProviderAsync();
        var source = new FileSystemLessonContentSource(provider, CreateOptions());

        LessonLibraryView library = await source.GetLibraryAsync();
        LessonContentDocument lesson = Assert.IsType<LessonContentDocument>(
            await source.GetLessonAsync("reference-types-001"));

        Assert.Equal(24, library.Modules.Count);
        Assert.All(library.Modules.Take(22), module => Assert.Equal(3, module.Lessons.Count));
        Assert.All(library.Modules.Skip(22), module => Assert.Equal(2, module.Lessons.Count));
        Assert.Contains(
            library.Modules.SelectMany(module => module.Lessons),
            summary => summary.Id == "reference-types-001");
        Assert.Equal(14, lesson.PublicView.Sections.Count);
        Assert.Equal("objectif", lesson.PublicView.Sections[0].Id);
        Assert.Equal("maitrise", lesson.PublicView.Sections[^1].Id);
        Assert.Equal(15, lesson.PublicView.ObservableActivityIds.Count);
        Assert.Equal(3, lesson.PublicView.Quiz.Options.Count);
        Assert.DoesNotContain(
            typeof(LessonQuizView).GetProperties(),
            property => property.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MarkdownParserKeepsHostileHtmlAsTextAndDisablesUnsafeLinks()
    {
        string reference = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "content",
            "reference",
            "curriculum",
            "lessons",
            "reference-types-001",
            "lesson.md"));
        string hostile = reference.Replace(
            "À la fin de cette leçon",
            "<script>alert('xss')</script> [piège](javascript:attack) À la fin de cette leçon",
            StringComparison.Ordinal);

        LessonParsedMarkdown parsed = SafeMarkdownLessonParser.Parse(hostile);
        LessonInlineView[] runs = parsed.Sections
            .SelectMany(section => section.Blocks)
            .OfType<LessonParagraphView>()
            .SelectMany(paragraph => paragraph.Inlines)
            .ToArray();

        Assert.Contains(runs, run => run.Text.Contains("<script>", StringComparison.Ordinal));
        Assert.DoesNotContain(
            runs,
            run => run.Kind == LessonInlineKind.Link
                && run.Href?.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) is true);
        Assert.Contains(runs, run => run.Text == "piège" && run.Kind == LessonInlineKind.Text);
    }

    [Fact]
    public async Task SearchFindsLessonBySkillWithoutChangingModuleOrder()
    {
        using ContentCatalogProvider provider = await CreateCatalogProviderAsync();
        var source = new FileSystemLessonContentSource(provider, CreateOptions());
        var browser = new BrowseLessons(provider, source);

        LessonLibraryView result = await browser.GetLibraryAsync("csharp.types");
        LessonLibraryView absent = await browser.GetLibraryAsync("compétence absente");

        Assert.Equal("reference-types-001", Assert.Single(Assert.Single(result.Modules).Lessons).Id);
        Assert.Empty(absent.Modules);
    }

    private static async Task<ContentCatalogProvider> CreateCatalogProviderAsync()
    {
        LessonContentOptions lessonOptions = CreateOptions();
        var validationOptions = new ContentValidationOptions
        {
            ContentRootPath = lessonOptions.ContentRootPath,
        };
        var validation = new FileSystemContentValidationService(validationOptions);
        var loader = new FileSystemContentCatalogLoader(validation, validationOptions);
        var provider = new ContentCatalogProvider(loader);
        ContentCatalogReloadResult result = await provider.ReloadAsync(lessonOptions.CatalogDirectoryPath);
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
        return provider;
    }

    private static LessonContentOptions CreateOptions()
    {
        string contentRoot = Path.Combine(RepositoryRoot, "content");
        return new LessonContentOptions
        {
            ContentRootPath = contentRoot,
            CatalogDirectoryPath = Path.Combine(contentRoot, "reference"),
        };
    }

    private static string RepositoryRoot
    {
        get
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
        }
    }
}
