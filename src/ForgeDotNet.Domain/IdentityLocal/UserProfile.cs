namespace ForgeDotNet.Domain.IdentityLocal;

public sealed record UserProfile
{
    public const int DefaultWeeklyHours = 10;
    public const int MinimumWeeklyHours = 1;
    public const int MaximumWeeklyHours = 40;

    private UserProfile(
        Guid localId,
        string displayName,
        string professionalGoal,
        int weeklyAvailableHours,
        InterfaceLanguage interfaceLanguage,
        DateTimeOffset createdAtUtc,
        bool hasAcceptedLearningContract)
    {
        LocalId = localId;
        DisplayName = displayName;
        ProfessionalGoal = professionalGoal;
        WeeklyAvailableHours = weeklyAvailableHours;
        InterfaceLanguage = interfaceLanguage;
        CreatedAtUtc = createdAtUtc;
        HasAcceptedLearningContract = hasAcceptedLearningContract;
    }

    public Guid LocalId { get; private init; }

    public string DisplayName { get; private init; }

    public string ProfessionalGoal { get; private init; }

    public int WeeklyAvailableHours { get; private init; }

    public InterfaceLanguage InterfaceLanguage { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public bool HasAcceptedLearningContract { get; private init; }

    public static UserProfile CreateDefault(DateTimeOffset createdAtUtc) =>
        new(
            Guid.NewGuid(),
            string.Empty,
            string.Empty,
            DefaultWeeklyHours,
            InterfaceLanguage.French,
            createdAtUtc,
            hasAcceptedLearningContract: false);

    public static UserProfile Restore(
        Guid localId,
        string displayName,
        string professionalGoal,
        int weeklyAvailableHours,
        InterfaceLanguage interfaceLanguage,
        DateTimeOffset createdAtUtc,
        bool hasAcceptedLearningContract)
    {
        if (localId == Guid.Empty)
        {
            throw new ArgumentException("L'identifiant local ne peut pas être vide.", nameof(localId));
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La date de création doit être exprimée en UTC.", nameof(createdAtUtc));
        }

        var restored = new UserProfile(
            localId,
            string.Empty,
            string.Empty,
            DefaultWeeklyHours,
            InterfaceLanguage.French,
            createdAtUtc,
            hasAcceptedLearningContract);

        if (string.IsNullOrEmpty(displayName) && string.IsNullOrEmpty(professionalGoal))
        {
            if (weeklyAvailableHours != DefaultWeeklyHours || interfaceLanguage != InterfaceLanguage.French)
            {
                throw new ArgumentException("Un profil initial vide contient des préférences incohérentes.");
            }

            return restored;
        }

        return restored.Update(displayName, professionalGoal, weeklyAvailableHours, interfaceLanguage);
    }

    public UserProfile Update(
        string displayName,
        string professionalGoal,
        int weeklyAvailableHours,
        InterfaceLanguage interfaceLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(professionalGoal);

        if (displayName.Trim().Length > 80)
        {
            throw new ArgumentOutOfRangeException(nameof(displayName), "Le pseudonyme ne peut pas dépasser 80 caractères.");
        }

        if (professionalGoal.Trim().Length > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(professionalGoal), "L'objectif ne peut pas dépasser 300 caractères.");
        }

        if (weeklyAvailableHours is < MinimumWeeklyHours or > MaximumWeeklyHours)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weeklyAvailableHours),
                $"Le nombre d'heures doit être compris entre {MinimumWeeklyHours} et {MaximumWeeklyHours}.");
        }

        if (!Enum.IsDefined(interfaceLanguage))
        {
            throw new ArgumentOutOfRangeException(nameof(interfaceLanguage));
        }

        return this with
        {
            DisplayName = displayName.Trim(),
            ProfessionalGoal = professionalGoal.Trim(),
            WeeklyAvailableHours = weeklyAvailableHours,
            InterfaceLanguage = interfaceLanguage,
        };
    }

    public UserProfile SetLearningContractAcceptance(bool accepted) =>
        this with { HasAcceptedLearningContract = accepted };
}
