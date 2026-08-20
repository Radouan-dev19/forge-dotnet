using System;

public static class Submission
{
    public static string FirstRejection(
        string token,
        string secret,
        string expectedIssuer,
        string expectedAudience,
        int nowUnixSeconds)
    {
        // Enchaînez les contrôles dans l'ordre du contrat — forme, algorithme, signature, puis
        // les revendications — et rendez le verdict du premier qui échoue.
        throw new NotImplementedException("La chaîne de validation reste à écrire.");
    }
}
