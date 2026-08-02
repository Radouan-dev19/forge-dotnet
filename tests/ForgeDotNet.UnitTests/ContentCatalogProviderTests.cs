using ForgeDotNet.Application.Content;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.UnitTests;

public sealed class ContentCatalogProviderTests
{
    [Fact]
    public async Task FailedReloadKeepsTheExactPreviousSnapshot()
    {
        ContentCatalog initial = CreateCatalog("initial", "lesson-initial");
        var loader = new StubLoader(ContentCatalogLoadResult.Failure([
            new ContentValidationIssue("missing-reference", "lesson.json", "$.prerequisites[0]", "Référence absente."),
        ]));
        using var provider = new ContentCatalogProvider(loader, initial);

        ContentCatalogReloadResult result = await provider.ReloadAsync("content");

        Assert.False(result.Succeeded);
        Assert.True(result.PreviousSnapshotPreserved);
        Assert.Same(initial, provider.Current);
    }

    [Fact]
    public async Task ReadersObserveOnlyCompleteSnapshotsDuringReload()
    {
        ContentCatalog initial = CreateCatalog("initial", "lesson-initial");
        ContentCatalog replacement = CreateCatalog("replacement", "lesson-replacement");
        var completion = new TaskCompletionSource<ContentCatalogLoadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = new ControlledLoader(completion.Task);
        using var provider = new ContentCatalogProvider(loader, initial);
        Task<ContentCatalogReloadResult> reload = provider.ReloadAsync("content");
        await loader.Started.Task;

        ContentCatalog[] beforePublication = Enumerable.Range(0, 1_000)
            .AsParallel()
            .Select(_ => provider.Current)
            .ToArray();
        completion.SetResult(ContentCatalogLoadResult.Success(replacement));
        ContentCatalogReloadResult result = await reload;
        ContentCatalog[] afterPublication = Enumerable.Range(0, 1_000)
            .AsParallel()
            .Select(_ => provider.Current)
            .ToArray();

        Assert.All(beforePublication, snapshot => Assert.Same(initial, snapshot));
        Assert.True(result.Succeeded);
        Assert.All(afterPublication, snapshot => Assert.Same(replacement, snapshot));
    }

    private static ContentCatalog CreateCatalog(string revision, string id) => new(
        revision,
        [new ContentCatalogItem(id, 1, ContentDocumentType.Lesson, id, "Résumé public", [], [])]);

    private sealed class StubLoader(ContentCatalogLoadResult result) : IContentCatalogLoader
    {
        public Task<ContentCatalogLoadResult> LoadAsync(
            string directoryPath,
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class ControlledLoader(Task<ContentCatalogLoadResult> result) : IContentCatalogLoader
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ContentCatalogLoadResult> LoadAsync(
            string directoryPath,
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            return await result.WaitAsync(cancellationToken);
        }
    }
}
