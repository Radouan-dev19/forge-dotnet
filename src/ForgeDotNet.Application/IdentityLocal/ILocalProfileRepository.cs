using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.IdentityLocal;

public interface ILocalProfileRepository
{
    ValueTask<UserProfile> GetAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
