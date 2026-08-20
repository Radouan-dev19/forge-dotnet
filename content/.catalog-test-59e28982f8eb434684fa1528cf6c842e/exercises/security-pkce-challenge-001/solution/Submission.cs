using System;
using System.Security.Cryptography;
using System.Text;

public static class Submission
{
    public static bool IsValidPkce(string codeVerifier, string codeChallenge)
    {
        // Bornes de la norme : l'entropie minimale de la preuve n'est pas négociable.
        if (string.IsNullOrEmpty(codeVerifier)
            || codeVerifier.Length < 43
            || codeVerifier.Length > 128)
        {
            return false;
        }

        foreach (char character in codeVerifier)
        {
            if (!IsUnreserved(character))
            {
                return false;
            }
        }

        if (string.IsNullOrEmpty(codeChallenge))
        {
            return false;
        }

        // S256 : condensat des octets ASCII, puis Base64 urlisé sans remplissage.
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        string recomputed = Convert.ToBase64String(hash)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        // Le guichet compare face à un client qui peut mesurer : temps constant.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(recomputed),
            Encoding.ASCII.GetBytes(codeChallenge));
    }

    private static bool IsUnreserved(char character) =>
        character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
            or '-' or '.' or '_' or '~';
}
