public static class Submission
{
    public static bool IsIdempotent(string method)
    {
        // Une méthode absente ou blanche n'est pas classable : refus calme.
        if (string.IsNullOrWhiteSpace(method))
        {
            return false;
        }

        // Normalisation d'identifiant technique : bords rognés, majuscules invariantes.
        string value = method.Trim().ToUpperInvariant();

        // Liste explicite des méthodes idempotentes ; POST et PATCH n'en font pas partie.
        return value is "GET" or "PUT" or "DELETE" or "HEAD" or "OPTIONS";
    }
}
