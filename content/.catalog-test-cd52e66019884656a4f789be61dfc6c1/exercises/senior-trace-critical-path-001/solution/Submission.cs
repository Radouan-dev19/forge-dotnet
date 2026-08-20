using System;
using System.Collections.Generic;

public static class Submission
{
    public static string SlowestSpan(string spans)
    {
        if (string.IsNullOrWhiteSpace(spans))
        {
            throw new ArgumentException("Une trace vide n'a pas de chemin à analyser.", nameof(spans));
        }

        var starts = new Dictionary<string, int>(StringComparer.Ordinal);
        var durations = new Dictionary<string, int>(StringComparer.Ordinal);
        var parents = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string span in spans.Split(';'))
        {
            string[] parts = span.Split(':');
            bool readable = parts.Length == 4 && parts[0].Length > 0 && parts[3].Length > 0
                && int.TryParse(parts[1], out int start) && start >= 0
                && int.TryParse(parts[2], out int end) && end >= start;
            if (!readable)
            {
                throw new ArgumentException("Un segment de la trace est illisible.", nameof(spans));
            }

            if (!durations.TryAdd(parts[0], int.Parse(parts[2]) - int.Parse(parts[1])))
            {
                throw new ArgumentException("Un nom de segment est répété.", nameof(spans));
            }

            starts.Add(parts[0], int.Parse(parts[1]));
            parents.Add(parts[0], parts[3]);
        }

        // Le temps propre retire de chaque parent la durée de ses enfants directs — un seul
        // niveau : la descendance est déjà comptée dans les enfants, la retirer compterait double.
        var selfTimes = new Dictionary<string, int>(durations, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> link in parents)
        {
            if (link.Value == "-")
            {
                continue;
            }

            // Un lien cassé attribuerait le temps d'un enfant à personne : la trace abîmée relève
            // du comptage d'orphelins, pas de l'analyse de chemin.
            if (!selfTimes.ContainsKey(link.Value))
            {
                throw new ArgumentException("Un parent cité n'existe pas dans la trace.", nameof(spans));
            }

            selfTimes[link.Value] -= durations[link.Key];
        }

        // Le départage — début le plus précoce puis ordre des noms — rend le verdict stable quel
        // que soit l'ordre du journal.
        string best = string.Empty;
        foreach (KeyValuePair<string, int> candidate in selfTimes)
        {
            bool better = best.Length == 0
                || candidate.Value > selfTimes[best]
                || (candidate.Value == selfTimes[best] && starts[candidate.Key] < starts[best])
                || (candidate.Value == selfTimes[best] && starts[candidate.Key] == starts[best]
                    && StringComparer.Ordinal.Compare(candidate.Key, best) < 0);
            if (better)
            {
                best = candidate.Key;
            }
        }

        return best + "|" + selfTimes[best];
    }
}
