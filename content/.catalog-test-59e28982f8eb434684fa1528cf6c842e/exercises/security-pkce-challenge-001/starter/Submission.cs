using System;

public static class Submission
{
    public static bool IsValidPkce(string codeVerifier, string codeChallenge)
    {
        // Validez le secret (bornes et alphabet), calculez son empreinte S256, puis
        // comparez-la en temps constant à celle qui a été déposée à l'aller.
        throw new NotImplementedException("La vérification du défi PKCE reste à écrire.");
    }
}
