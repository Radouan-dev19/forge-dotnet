using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Content;

public sealed class ContentCatalogProvider : IDisposable
{
    private readonly IContentCatalogLoader _loader;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private ContentCatalog _current;
    private bool _disposed;

    public ContentCatalogProvider(
        IContentCatalogLoader loader,
        ContentCatalog? initialCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
        _current = initialCatalog ?? ContentCatalog.Empty;
    }

    public ContentCatalog Current => Volatile.Read(ref _current);

    public async Task<ContentCatalogReloadResult> ReloadAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ContentCatalog previous = Current;
            ContentCatalogLoadResult result = await _loader
                .LoadAsync(directoryPath, cancellationToken)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return new ContentCatalogReloadResult(false, previous, previous, result.Issues);
            }

            ContentCatalog candidate = result.Catalog!;
            Volatile.Write(ref _current, candidate);
            return new ContentCatalogReloadResult(true, previous, candidate, []);
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _reloadGate.Dispose();
        _disposed = true;
    }
}
