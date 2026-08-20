using System;

public static class Submission
{
    public static bool IsForAudience(string token, string expectedAudience)
    {
        // Décodez la charge utile, puis décidez selon la forme JSON de la revendication
        // d'audience : chaîne unique, tableau de chaînes, ou forme à refuser.
        throw new NotImplementedException("Le contrôle d'audience reste à écrire.");
    }
}
