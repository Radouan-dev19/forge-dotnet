using ForgeDotNet.Domain.Labs;

namespace ForgeDotNet.Application.Labs;

/// <summary>
/// Source des laboratoires publiés, confinée au serveur.
/// </summary>
public interface ILabSource
{
    ValueTask<IReadOnlyList<Lab>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<Lab?> GetAsync(string labId, CancellationToken cancellationToken = default);
}
