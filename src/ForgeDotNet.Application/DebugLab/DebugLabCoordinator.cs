namespace ForgeDotNet.Application.DebugLab;

public sealed class DebugLabCoordinator : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async ValueTask<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Lease(_gate);
    }

    public void Dispose() => _gate.Dispose();

    private sealed class Lease(SemaphoreSlim gate) : IAsyncDisposable
    {
        private SemaphoreSlim? _gate = gate;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _gate, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
