using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.IdentityLocal;

public sealed record UpdateLocalProfileCommand(
    string DisplayName,
    string ProfessionalGoal,
    int WeeklyAvailableHours,
    InterfaceLanguage InterfaceLanguage);

public sealed class UpdateLocalProfile(ILocalProfileRepository repository)
{
    public async ValueTask<UserProfile> ExecuteAsync(
        UpdateLocalProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentProfile = await repository.GetAsync(cancellationToken);
        var updatedProfile = currentProfile.Update(
            command.DisplayName,
            command.ProfessionalGoal,
            command.WeeklyAvailableHours,
            command.InterfaceLanguage);

        await repository.SaveAsync(updatedProfile, cancellationToken);
        return updatedProfile;
    }
}
