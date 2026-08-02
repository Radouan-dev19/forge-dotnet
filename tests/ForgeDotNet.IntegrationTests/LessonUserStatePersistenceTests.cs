using ForgeDotNet.Application.Content;
using ForgeDotNet.Application.Curriculum;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.Curriculum;

namespace ForgeDotNet.IntegrationTests;

public sealed class LessonUserStatePersistenceTests
{
    [Fact]
    public async Task VisitingAndWrongQuizAnswerDoNotCreateProgressButCorrectAnswerDoes()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        using ContentCatalogProvider provider = await CreateCatalogProviderAsync();
        var source = new FileSystemLessonContentSource(provider, CreateContentOptions());
        var browser = new BrowseLessons(provider, source);
        ILocalProfileRepository profiles = environment.GetRequiredService<ILocalProfileRepository>();
        ILessonUserStateRepository states = environment.GetRequiredService<ILessonUserStateRepository>();
        var getState = new GetLessonReaderState(browser, profiles, states);
        var submitQuiz = new SubmitLessonQuiz(browser, source, profiles, states);

        LessonReaderState initial = await getState.ExecuteAsync("reference-types-001");
        LessonQuizResult wrong = await submitQuiz.ExecuteAsync("reference-types-001", 0);
        LessonQuizResult correct = await submitQuiz.ExecuteAsync("reference-types-001", 1);

        Assert.Equal(0, initial.ProgressPercentage);
        Assert.False(wrong.IsCorrect);
        Assert.Equal(0, wrong.State.ProgressPercentage);
        Assert.True(correct.IsCorrect);
        Assert.Equal(7, correct.State.ProgressPercentage);
        Assert.Equal(["quiz:money-type-check"], correct.State.CompletedActivityIds);
    }

    [Fact]
    public async Task NoteBookmarkAndObservedActivitySurviveAServiceRestart()
    {
        string dataDirectory;
        Guid profileId;

        await using (var firstRun = await PersistenceTestEnvironment.CreateAsync(deleteOnDispose: false))
        {
            dataDirectory = firstRun.DataDirectory;
            profileId = (await firstRun
                .GetRequiredService<ILocalProfileRepository>()
                .GetAsync()).LocalId;
            ILessonUserStateRepository repository = firstRun
                .GetRequiredService<ILessonUserStateRepository>();
            await repository.SaveNoteAsync(profileId, "reference-types-001", "Comparer les arrondis par ligne.");
            await repository.SetBookmarkAsync(profileId, "reference-types-001", isBookmarked: true);
            await repository.AddCompletedActivityAsync(
                profileId,
                "reference-types-001",
                "section:objectif");
        }

        await using var secondRun = await PersistenceTestEnvironment.CreateAsync(dataDirectory);
        LessonUserStateSnapshot state = await secondRun
            .GetRequiredService<ILessonUserStateRepository>()
            .GetAsync(profileId, "reference-types-001");

        Assert.Equal("Comparer les arrondis par ligne.", state.Note);
        Assert.True(state.IsBookmarked);
        Assert.Equal(["section:objectif"], state.CompletedActivityIds);
    }

    [Fact]
    public async Task RepeatingAnActivityDoesNotCreateFalseProgressRows()
    {
        await using var environment = await PersistenceTestEnvironment.CreateAsync();
        Guid profileId = (await environment
            .GetRequiredService<ILocalProfileRepository>()
            .GetAsync()).LocalId;
        ILessonUserStateRepository repository = environment
            .GetRequiredService<ILessonUserStateRepository>();

        await repository.AddCompletedActivityAsync(profileId, "reference-types-001", "section:objectif");
        await repository.AddCompletedActivityAsync(profileId, "reference-types-001", "section:objectif");
        LessonUserStateSnapshot state = await repository.GetAsync(profileId, "reference-types-001");

        Assert.Equal(["section:objectif"], state.CompletedActivityIds);
    }

    private static async Task<ContentCatalogProvider> CreateCatalogProviderAsync()
    {
        LessonContentOptions lessonOptions = CreateContentOptions();
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

    private static LessonContentOptions CreateContentOptions()
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
