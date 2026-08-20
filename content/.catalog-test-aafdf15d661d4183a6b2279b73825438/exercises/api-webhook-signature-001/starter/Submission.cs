using System;

public static class Submission
{
    public static bool IsWebhookSignatureValid(
        int timestamp,
        string rawBody,
        string secret,
        string presentedSignatureHex)
    {
        // Reconstituez la chaîne signée horodatage.corps, recalculez le condensat HMAC,
        // décodez la signature présentée et comparez en temps constant.
        throw new NotImplementedException("La vérification de signature du webhook reste à écrire.");
    }
}
