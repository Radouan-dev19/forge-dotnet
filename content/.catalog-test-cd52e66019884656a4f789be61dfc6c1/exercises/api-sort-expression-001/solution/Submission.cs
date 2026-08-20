using System;
using System.Collections.Generic;

public static class Submission
{
    // Le départage est toujours triable : c'est lui qui rend l'ordre total, donc la pagination stable.
    private const string TieBreaker = "id";

    public static string NormalizeSort(string expression, string allowed)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(allowed);

        List<string> whitelist = SplitList(allowed);
        if (whitelist.Count == 0)
        {
            throw new ArgumentException("Aucun champ triable n'est déclaré.", nameof(allowed));
        }

        List<string> terms = SplitList(expression);
        var ordered = new List<string>();
        var seen = new List<string>();

        foreach (string term in terms)
        {
            (string field, bool descending) = ReadTerm(term);

            // Le refus est explicite : un tri silencieusement ignoré donne au client des données
            // qu'il croit triées.
            string resolved = Resolve(field, whitelist)
                ?? throw new ArgumentException(
                    "Le champ demandé n'est pas triable.", nameof(expression));

            if (Contains(seen, resolved))
            {
                continue;
            }

            seen.Add(resolved);
            ordered.Add(resolved + (descending ? ":desc" : ":asc"));
        }

        if (ordered.Count == 0)
        {
            ordered.Add(whitelist[0] + ":asc");
            seen.Add(whitelist[0]);
        }

        if (!Contains(seen, TieBreaker))
        {
            ordered.Add(TieBreaker + ":asc");
        }

        return string.Join(",", ordered);
    }

    /// <summary>
    /// Lit un terme dans l'une des deux écritures acceptées et rend le champ demandé avec son sens.
    /// </summary>
    private static (string Field, bool Descending) ReadTerm(string term)
    {
        if (term.StartsWith('-'))
        {
            string prefixed = term[1..].Trim();
            if (prefixed.Length == 0)
            {
                throw new ArgumentException("Un terme de tri est vide.", nameof(term));
            }

            return (prefixed, true);
        }

        int space = term.IndexOf(' ', StringComparison.Ordinal);
        if (space < 0)
        {
            return (term, false);
        }

        string field = term[..space].Trim();
        string direction = term[(space + 1)..].Trim();
        if (string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase))
        {
            return (field, false);
        }

        if (string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return (field, true);
        }

        throw new ArgumentException("Le sens de tri demandé est inconnu.", nameof(term));
    }

    /// <summary>
    /// Rend l'orthographe déclarée du champ, ou null s'il n'est pas triable. Le départage échappe à
    /// la liste blanche : il ne vient jamais du client mais de la règle de pagination.
    /// </summary>
    private static string? Resolve(string field, List<string> whitelist)
    {
        if (string.Equals(field, TieBreaker, StringComparison.OrdinalIgnoreCase))
        {
            return TieBreaker;
        }

        foreach (string candidate in whitelist)
        {
            if (string.Equals(candidate, field, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool Contains(List<string> values, string value)
    {
        foreach (string candidate in values)
        {
            if (string.Equals(candidate, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> SplitList(string value)
    {
        var items = new List<string>();
        foreach (string part in value.Split(','))
        {
            string item = part.Trim();
            if (item.Length > 0)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
