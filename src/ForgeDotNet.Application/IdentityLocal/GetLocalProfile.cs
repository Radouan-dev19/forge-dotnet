using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.IdentityLocal;

public sealed class GetLocalProfile(ILocalProfileRepository repository)
{
    public ValueTask<UserProfile> ExecuteAsync(CancellationToken cancellationToken = default) =>
        repository.GetAsync(cancellationToken);
}
