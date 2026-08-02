using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.Application.DebugLab;

public interface IDebugLabRepository
{
    ValueTask<DebugLabActivity?> GetAsync(Guid profileId, string scenarioId, CancellationToken cancellationToken = default);

    ValueTask<DebugLabActivity> CreateOrGetAsync(DebugLabActivity activity, CancellationToken cancellationToken = default);

    ValueTask<DebugLabActivity> SaveAsync(DebugLabActivity activity, int expectedVersion, CancellationToken cancellationToken = default);
}
