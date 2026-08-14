using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ProcessOutcomes(string keys)
    {
        // Une entree nulle est une absence de liste, pas une liste vide : on la refuse.
        ArgumentNullException.ThrowIfNull(keys);

        // Les segments vides sont ignores : ils ne fabriquent pas de cle fantome.
        string[] segments = keys.Split(';', StringSplitOptions.RemoveEmptyEntries);

        // La memoire des cles deja rencontrees porte toute la decision.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var outcomes = new List<string>(segments.Length);

        foreach (string key in segments)
        {
            // On teste la presence AVANT d'inserer : sinon la premiere apparition passerait
            // pour un rejeu. Add rend faux quand la cle etait deja connue.
            bool firstTime = seen.Add(key);
            outcomes.Add(firstTime ? "processed" : "replayed");
        }

        // L'ordre de lecture est preserve : la sortie suit le parcours, pas l'ensemble.
        return string.Join(";", outcomes);
    }
}
