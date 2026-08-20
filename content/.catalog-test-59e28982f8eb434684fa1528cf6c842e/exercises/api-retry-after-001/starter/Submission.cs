using System;

public static class Submission
{
    public static int RetryAfterSeconds(
        int requestCount,
        int quota,
        int nowUnix,
        int windowSeconds,
        int windowStartUnix)
    {
        // Décidez d'abord si le client est limité, puis calculez le délai jusqu'au
        // début de la fenêtre suivante, borné à zéro par le bas.
        throw new NotImplementedException("Le calcul du Retry-After reste à écrire.");
    }
}
