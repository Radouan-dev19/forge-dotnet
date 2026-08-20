using System;

public static class Submission
{
    public static bool IsSignatureValid(string token, string secret)
    {
        // Recalculez le condensat HMAC-SHA256 côté vérificateur, puis comparez-le en temps
        // constant à celui que porte le troisième segment du jeton.
        throw new NotImplementedException("La vérification de signature reste à écrire.");
    }
}
