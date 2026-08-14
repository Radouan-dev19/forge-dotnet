using System;

public static class Submission
{
    public static string CacheDirective(string responseKind, int maxAgeSeconds)
    {
        // Une durée de fraîcheur négative n'a pas de sens.
        if (maxAgeSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAgeSeconds));
        }

        string kind = (responseKind ?? "").Trim().ToLowerInvariant();

        // La sensibilité prime : rien à stocker, et donc aucune durée n'apparaît.
        if (kind == "sensitive")
        {
            return "no-store";
        }

        // Personnel : seul le cache du client final peut garder la réponse.
        if (kind == "personal")
        {
            return $"private, max-age={maxAgeSeconds}";
        }

        // Public : les caches partagés peuvent la servir à tous.
        if (kind == "public")
        {
            return $"public, max-age={maxAgeSeconds}";
        }

        // Nature inconnue : présomption de prudence, on ne met pas en cache.
        return "no-store";
    }
}
