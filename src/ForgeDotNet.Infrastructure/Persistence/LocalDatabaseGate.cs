namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class LocalDatabaseGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async ValueTask<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        private bool _isReleased;

        public ValueTask DisposeAsync()
        {
            if (!_isReleased)
            {
                semaphore.Release();
                _isReleased = true;
            }

            return ValueTask.CompletedTask;
        }
    }
}
