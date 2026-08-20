using System;
using System.Security.Cryptography;
using System.Text;

public static class Submission
{
    public static string ComputeETag(string representation)
    {
        // Le condensat travaille sur des octets : on encode explicitement en UTF-8
        // plutôt que de dépendre de la représentation interne de la chaîne.
        byte[] bytes = Encoding.UTF8.GetBytes(representation ?? "");
        byte[] hash = SHA256.HashData(bytes);

        // Hexadécimal minuscule : casse fixée pour que deux calculs coïncident toujours.
        string hex = Convert.ToHexString(hash).ToLowerInvariant();

        // ETag fort : encadré de guillemets doubles, sans préfixe de faiblesse.
        return $"\"{hex}\"";
    }
}
