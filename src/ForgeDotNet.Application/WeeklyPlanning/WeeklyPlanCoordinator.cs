namespace ForgeDotNet.Application.WeeklyPlanning;

public sealed class WeeklyPlanCoordinator : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async ValueTask<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Lease(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Lease(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        private bool _released;

        public ValueTask DisposeAsync()
        {
            if (!_released)
            {
                semaphore.Release();
                _released = true;
            }

            return ValueTask.CompletedTask;
        }
    }
}
