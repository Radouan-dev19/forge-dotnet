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
        // Une fenêtre de durée nulle ou négative ne décrit aucune limite.
        if (windowSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSeconds));
        }

        // Sous le quota : le client n'est pas limité, rien à attendre.
        if (requestCount < quota)
        {
            return 0;
        }

        // Limité jusqu'à la réinitialisation : début de la fenêtre suivante moins maintenant.
        long nextWindowStart = (long)windowStartUnix + windowSeconds;
        long delay = nextWindowStart - nowUnix;

        // Un Retry-After négatif n'a pas de sens : la fenêtre est passée, réessayer est permis.
        return delay > 0 ? (int)delay : 0;
    }
}
