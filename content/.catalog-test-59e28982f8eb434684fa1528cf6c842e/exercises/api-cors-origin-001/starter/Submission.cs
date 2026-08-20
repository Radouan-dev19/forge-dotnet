using System;

public static class Submission
{
    public static string ResolveAllowedOrigin(string requestOrigin, string allowlist, bool withCredentials)
    {
        // Le joker n'est permis que sans identifiants ; sinon, seule une origine nommée
        // et confrontée à la liste peut être autorisée. Jamais d'écho aveugle.
        throw new NotImplementedException("La résolution d'origine reste à écrire.");
    }
}
