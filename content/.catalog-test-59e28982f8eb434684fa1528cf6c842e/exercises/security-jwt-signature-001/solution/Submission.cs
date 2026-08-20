using System;
using System.Security.Cryptography;
using System.Text;

public static class Submission
{
    public static bool IsSignatureValid(string token, string secret)
    {
        if (string.IsNullOrEmpty(token) || secret is null)
        {
            return false;
        }

        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return false;
        }

        byte[] presented;
        try
        {
            presented = DecodeBase64Url(segments[2]);
        }
        catch (FormatException)
        {
            // Signature indécodable : c'est un refus ordinaire, pas une erreur du vérificateur.
            return false;
        }

        // L'algorithme est décidé ici même : HMAC-SHA256, quoi que l'en-tête du jeton annonce.
        byte[] signedBytes = Encoding.UTF8.GetBytes(segments[0] + "." + segments[1]);
        byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signedBytes);

        // Temps constant : la durée de comparaison ne révèle pas le point de divergence.
        return CryptographicOperations.FixedTimeEquals(expected, presented);
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        string base64 = segment.Replace('-', '+').Replace('_', '/');

        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Longueur impossible pour du Base64.");
        }

        if (remainder == 2)
        {
            base64 += "==";
        }
        else if (remainder == 3)
        {
            base64 += "=";
        }

        return Convert.FromBase64String(base64);
    }
}
