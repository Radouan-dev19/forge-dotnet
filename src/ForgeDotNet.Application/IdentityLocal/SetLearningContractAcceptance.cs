using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.IdentityLocal;

public sealed class SetLearningContractAcceptance(ILocalProfileRepository repository)
{
    public async ValueTask<UserProfile> ExecuteAsync(
        bool accepted,
        CancellationToken cancellationToken = default)
    {
        var currentProfile = await repository.GetAsync(cancellationToken);
        var updatedProfile = currentProfile.SetLearningContractAcceptance(accepted);
        await repository.SaveAsync(updatedProfile, cancellationToken);
        return updatedProfile;
    }
}
