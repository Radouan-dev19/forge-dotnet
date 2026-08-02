namespace ForgeDotNet.Domain.IdentityLocal;

public static class LearningContract
{
    public static IReadOnlyList<string> Commitments { get; } =
    [
        "Travailler régulièrement.",
        "Déclarer honnêtement l’usage des indices.",
        "Ne pas considérer une solution consultée comme maîtrisée.",
        "Effectuer des séances sans IA.",
        "Accepter que la plateforme mesure les lacunes sans flatter artificiellement.",
    ];

    public static bool IsLearningPathActivated(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.HasAcceptedLearningContract;
    }
}
