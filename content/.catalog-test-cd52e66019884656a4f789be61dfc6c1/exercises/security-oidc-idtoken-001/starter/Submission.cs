using System;

public static class Submission
{
    public static string IdTokenVerdict(
        string idToken,
        string expectedNonce,
        string expectedClientId,
        string accessToken)
    {
        // Décodez la charge utile, puis enchaînez les trois liens dans l'ordre du
        // contrat : la demande, le client, le jeton d'accès reçu ensemble.
        throw new NotImplementedException("La validation du jeton d'identité reste à écrire.");
    }
}
