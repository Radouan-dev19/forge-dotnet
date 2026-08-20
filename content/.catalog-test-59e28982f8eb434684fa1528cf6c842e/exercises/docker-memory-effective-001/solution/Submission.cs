using System;
using System.Collections.Generic;

public static class Submission
{
    // Rang de proximité : plus le rang est haut, plus la source est proche du conteneur.
    private static readonly Dictionary<string, int> Proximity = new(StringComparer.Ordinal)
    {
        ["daemon"] = 0,
        ["cgroup-parent"] = 1,
        ["compose"] = 2,
        ["run"] = 3,
    };

    public static string EffectiveMemoryLimit(string constraints)
    {
        // L'absence de contrainte est l'absence de paire : la chaîne vide est un état légitime.
        if (constraints.Length == 0)
        {
            return "unlimited";
        }

        var limits = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string pair in constraints.Split(';'))
        {
            string[] parts = pair.Split('=');
            bool readable = parts.Length == 2
                && Proximity.ContainsKey(parts[0])
                && int.TryParse(parts[1], out int declared)
                && declared > 0;
            if (!readable)
            {
                throw new ArgumentException("Une contrainte de la chaîne est illisible.", nameof(constraints));
            }

            // Deux valeurs pour le même étage : un fichier fusionné qui a mal tourné, pas un choix.
            if (!limits.TryAdd(parts[0], int.Parse(parts[1])))
            {
                throw new ArgumentException("Une source de contrainte est répétée.", nameof(constraints));
            }
        }

        int effective = int.MaxValue;
        string source = string.Empty;
        foreach (KeyValuePair<string, int> limit in limits)
        {
            // Les plafonds s'empilent : seul le plus bas agit. À égalité, la source la plus proche
            // du conteneur est rapportée, parce que c'est le levier le moins cher à ajuster.
            bool lower = limit.Value < effective;
            bool closerAtSameValue = limit.Value == effective
                && (source.Length == 0 || Proximity[limit.Key] > Proximity[source]);
            if (lower || closerAtSameValue)
            {
                effective = limit.Value;
                source = limit.Key;
            }
        }

        return effective + "|" + source;
    }
}
