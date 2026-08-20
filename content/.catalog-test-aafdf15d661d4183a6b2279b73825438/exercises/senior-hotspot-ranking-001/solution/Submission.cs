using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string TopHotspots(string files)
    {
        if (string.IsNullOrWhiteSpace(files))
        {
            throw new ArgumentException("Un relevé vide ne désigne aucun point chaud.", nameof(files));
        }

        var scores = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (string entry in files.Split(';'))
        {
            string[] parts = entry.Split(':');
            bool readable = parts.Length == 3 && parts[0].Length > 0
                && int.TryParse(parts[1], out int churn) && churn >= 0
                && int.TryParse(parts[2], out int complexity) && complexity >= 0;
            if (!readable)
            {
                throw new ArgumentException("Une entrée du relevé est illisible.", nameof(files));
            }

            // Produit en entier large : le débordement inverserait le classement en silence,
            // précisément sur les monstres que l'analyse existe pour trouver.
            if (!scores.TryAdd(parts[0], (long)int.Parse(parts[1]) * int.Parse(parts[2])))
            {
                throw new ArgumentException("Un fichier du relevé est répété.", nameof(files));
            }
        }

        // Podium : trois noms déclenchent une action, un inventaire déclenche une réunion.
        // Le départage par nom garde le rapport comparable d'un trimestre à l'autre.
        var podium = scores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(3)
            .Select(pair => pair.Key);

        return string.Join(';', podium);
    }
}
