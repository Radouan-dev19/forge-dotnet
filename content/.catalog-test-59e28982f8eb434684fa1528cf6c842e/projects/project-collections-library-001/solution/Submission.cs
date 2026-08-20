using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string Normalize(string values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (string value in Sanitize(values))
        {
            if (seen.Add(value))
            {
                result.Add(value);
            }
        }

        return string.Join(";", result);
    }

    public static Dictionary<string, int> Frequencies(string values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string value in Sanitize(values))
        {
            counts[value] = counts.TryGetValue(value, out int current) ? current + 1 : 1;
        }

        return counts;
    }

    public static string TopValues(string values, int count)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (count <= 0)
        {
            return string.Empty;
        }

        // La fréquence classe ; l'ordre ordinal ne tranche que les égalités, ce qui rend le
        // résultat reproductible quel que soit l'ordre d'arrivée des valeurs.
        return string.Join(
            ";",
            Frequencies(values)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(count)
                .Select(pair => pair.Key));
    }

    // La règle d'assainissement vit à un seul endroit : la corriger corrige les trois méthodes.
    private static List<string> Sanitize(string values)
    {
        var result = new List<string>();
        foreach (string segment in values.Split(';'))
        {
            string value = segment.Trim();
            if (value.Length > 0)
            {
                result.Add(value);
            }
        }

        return result;
    }
}
