using System;

public static class Submission
{
    public static string CacheDecision(string entry, int nowSeconds, int staleAfter, int expireAfter)
    {
        const string Prefix = "storedAt=";

        // Une entree absente ou sans le prefixe attendu n'a pas d'age calculable.
        if (entry is null || !entry.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Entree de cache mal formee.", nameof(entry));
        }

        // Ce qui suit le prefixe doit etre un entier, sinon l'entree reste mal formee.
        if (!int.TryParse(entry.Substring(Prefix.Length), out int storedAt))
        {
            throw new ArgumentException("Instant de stockage illisible.", nameof(entry));
        }

        // Des seuils negatifs ou inverses rendraient la zone intermediaire impossible.
        if (staleAfter < 0 || expireAfter < 0 || staleAfter > expireAfter)
        {
            throw new ArgumentException("Seuils de cache incoherents.");
        }

        // L'age peut etre negatif si l'horloge d'ecriture devance celle de lecture.
        int age = nowSeconds - storedAt;

        // Zone basse, borne haute exclue : un age negatif y tombe naturellement, donc frais.
        if (age < staleAfter)
        {
            return "fresh";
        }

        // Zone intermediaire : staleAfter inclus, expireAfter exclu.
        if (age < expireAfter)
        {
            return "stale-revalidate";
        }

        // Reste le cas age superieur ou egal a expireAfter : l'entree est perimee.
        return "expired";
    }
}
