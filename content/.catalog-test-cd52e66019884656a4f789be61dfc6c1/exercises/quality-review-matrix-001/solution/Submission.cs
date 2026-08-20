using System;
using System.Collections.Generic;

public static class Submission
{
    private static readonly Dictionary<string, int> CategoryPoints = new(StringComparer.Ordinal)
    {
        ["security"] = 4,
        ["correctness"] = 3,
        ["performance"] = 2,
        ["style"] = 1,
    };

    private static readonly Dictionary<string, int> ReachablePoints = new(StringComparer.Ordinal)
    {
        ["always"] = 2,
        ["feature-flag"] = 1,
        ["dead-code"] = 0,
    };

    private static readonly Dictionary<string, int> BlastPoints = new(StringComparer.Ordinal)
    {
        ["system"] = 2,
        ["module"] = 1,
        ["line"] = 0,
    };

    public static string ReviewSeverity(string finding)
    {
        if (string.IsNullOrWhiteSpace(finding))
        {
            throw new ArgumentException("Un constat vide ne se gradue pas.", nameof(finding));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in finding.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Une paire du constat est illisible.", nameof(finding));
            }

            // Une clé répétée ferait dépendre le verdict de l'ordre d'écriture : refus immédiat.
            if (!attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut du constat est répété.", nameof(finding));
            }
        }

        // Le barème est toute la règle : aucun attribut deviné, aucune exception codée en dur.
        int points = PointsOf(attributes, "category", CategoryPoints)
            + PointsOf(attributes, "reachable", ReachablePoints)
            + PointsOf(attributes, "blast", BlastPoints);

        if (attributes.Count != 3)
        {
            throw new ArgumentException("Un attribut du constat est hors vocabulaire.", nameof(finding));
        }

        return points switch
        {
            >= 7 => "blocker",
            >= 5 => "major",
            >= 3 => "minor",
            _ => "nit",
        };
    }

    private static int PointsOf(Dictionary<string, string> attributes, string key, Dictionary<string, int> table)
    {
        if (!attributes.TryGetValue(key, out string? value))
        {
            throw new ArgumentException("Un attribut du constat est absent.", nameof(attributes));
        }

        if (!table.TryGetValue(value, out int points))
        {
            throw new ArgumentException("Une valeur du constat est hors vocabulaire.", nameof(attributes));
        }

        return points;
    }
}
