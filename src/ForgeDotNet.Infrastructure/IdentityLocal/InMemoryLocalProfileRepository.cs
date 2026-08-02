using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Infrastructure.IdentityLocal;

public sealed class InMemoryLocalProfileRepository : ILocalProfileRepository
{
    private readonly Lock _syncRoot = new();
    private UserProfile _profile = UserProfile.CreateDefault(DateTimeOffset.UtcNow);

    public ValueTask<UserProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            return ValueTask.FromResult(_profile);
        }
    }

    public ValueTask SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _profile = profile;
        }

        return ValueTask.CompletedTask;
    }
}
