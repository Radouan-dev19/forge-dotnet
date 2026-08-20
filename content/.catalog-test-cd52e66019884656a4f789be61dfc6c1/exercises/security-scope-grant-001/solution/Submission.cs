using System;

public static class Submission
{
    public static bool IsGranted(string granted, string required)
    {
        ArgumentNullException.ThrowIfNull(granted);
        ArgumentNullException.ThrowIfNull(required);

        string target = required.Trim();
        if (target.Length == 0)
        {
            throw new ArgumentException("La portée exigée est vide.", nameof(required));
        }

        if (target.Contains('*', StringComparison.Ordinal))
        {
            throw new ArgumentException("La portée exigée doit être concrète.", nameof(required));
        }

        bool allowed = false;
        foreach (string rawEntry in granted.Split(' '))
        {
            string entry = rawEntry.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            bool denial = entry.StartsWith('!');
            string scope = denial ? entry[1..] : entry;
            if (!Matches(scope, target))
            {
                continue;
            }

            // Le refus court-circuite : le parcours n'a plus rien à apprendre, et aucune
            // autorisation rencontrée plus loin ne pourra le contredire.
            if (denial)
            {
                return false;
            }

            allowed = true;
        }

        // Moindre privilège : l'absence de correspondance n'est pas une absence de règle.
        return allowed;
    }

    /// <summary>
    /// Vrai lorsque l'entrée du jeton couvre la portée exigée. La comparaison est sensible à la
    /// casse : une portée est un identifiant, et deux graphies ne désignent pas le même droit.
    /// </summary>
    private static bool Matches(string scope, string target)
    {
        if (string.Equals(scope, "*", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(scope, target, StringComparison.Ordinal))
        {
            return true;
        }

        if (!scope.EndsWith(":*", StringComparison.Ordinal))
        {
            return false;
        }

        // Le préfixe conserve les deux points : « orders:* » couvre « orders:read », jamais
        // « orders » seul ni « ordersextra:read ».
        string prefix = scope[..^1];
        return target.Length > prefix.Length
            && target.StartsWith(prefix, StringComparison.Ordinal);
    }
}
