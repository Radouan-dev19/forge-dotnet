using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public static class Submission
{
    public static bool IsWebhookSignatureValid(
        int timestamp,
        string rawBody,
        string secret,
        string presentedSignatureHex)
    {
        if (rawBody is null || secret is null || string.IsNullOrEmpty(presentedSignatureHex))
        {
            return false;
        }

        // La chaîne signée lie l'horodatage au corps BRUT : on ne re-sérialise jamais.
        string signedContent = timestamp.ToString(CultureInfo.InvariantCulture) + "." + rawBody;
        byte[] expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(signedContent));

        byte[] presented;
        try
        {
            presented = Convert.FromHexString(presentedSignatureHex);
        }
        catch (FormatException)
        {
            // Signature malformée : refus ordinaire, pas une erreur du vérificateur.
            return false;
        }

        // Longueurs incohérentes puis comparaison en temps constant : la durée ne
        // révèle pas le point de divergence à un émetteur qui mesurerait.
        return presented.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(expected, presented);
    }
}
