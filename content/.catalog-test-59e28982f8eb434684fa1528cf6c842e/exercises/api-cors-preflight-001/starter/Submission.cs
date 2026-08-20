using System;

public static class Submission
{
    public static bool IsPreflightAllowed(
        string requestedMethod,
        string allowedMethods,
        string requestedHeaders,
        string allowedHeaders)
    {
        // Confirmez la méthode ET chaque en-tête demandé : un seul en-tête non autorisé
        // suffit à refuser le préflight entier.
        throw new NotImplementedException("La décision de préflight reste à écrire.");
    }
}
