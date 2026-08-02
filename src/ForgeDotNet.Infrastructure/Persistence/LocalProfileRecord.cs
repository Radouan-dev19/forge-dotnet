using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class LocalProfileRecord
{
    public const int OnlyProfileSlot = 1;

    public int ProfileSlot { get; set; } = OnlyProfileSlot;

    public Guid LocalId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string ProfessionalGoal { get; set; } = string.Empty;

    public int WeeklyAvailableHours { get; set; }

    public InterfaceLanguage InterfaceLanguage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public bool HasAcceptedLearningContract { get; set; }

    public static LocalProfileRecord FromDomain(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new LocalProfileRecord
        {
            LocalId = profile.LocalId,
            DisplayName = profile.DisplayName,
            ProfessionalGoal = profile.ProfessionalGoal,
            WeeklyAvailableHours = profile.WeeklyAvailableHours,
            InterfaceLanguage = profile.InterfaceLanguage,
            CreatedAtUtc = profile.CreatedAtUtc,
            HasAcceptedLearningContract = profile.HasAcceptedLearningContract,
        };
    }

    public UserProfile ToDomain() => UserProfile.Restore(
        LocalId,
        DisplayName,
        ProfessionalGoal,
        WeeklyAvailableHours,
        InterfaceLanguage,
        CreatedAtUtc,
        HasAcceptedLearningContract);

    public void Apply(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (LocalId != profile.LocalId)
        {
            throw new InvalidOperationException("L'identité du profil local ne peut pas être remplacée.");
        }

        DisplayName = profile.DisplayName;
        ProfessionalGoal = profile.ProfessionalGoal;
        WeeklyAvailableHours = profile.WeeklyAvailableHours;
        InterfaceLanguage = profile.InterfaceLanguage;
        HasAcceptedLearningContract = profile.HasAcceptedLearningContract;
    }
}
