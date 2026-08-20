using System;

public static class Submission
{
    public static bool IsBreakingChange(string changeKind)
    {
        // Une étiquette absente tombe du côté cassant par le défaut : pas d'information
        // de sûreté, donc pas de sûreté présumée.
        string normalized = (changeKind ?? "").Trim().ToLowerInvariant();

        // Liste fermée des additions strictes — les seules qui préservent les appels
        // en place. Tout le reste, y compris l'inconnu, est présumé cassant.
        return normalized switch
        {
            "add-optional-input" => false,
            "add-output-field" => false,
            "add-endpoint" => false,
            _ => true,
        };
    }
}
