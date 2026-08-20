using System;
using System.Collections.Generic;

public static class Submission
{
    public static string FirstBlockingJob(string log)
    {
        if (string.IsNullOrWhiteSpace(log))
        {
            throw new ArgumentException("Un journal vide ne décrit aucun pipeline.", nameof(log));
        }

        // La liste fige l'ordre de première apparition — le dictionnaire ne le garantit pas — et la
        // valeur, réécrite à chaque entrée, consolide le verdict final : une relance réussie efface
        // l'échec qui la précède.
        var order = new List<string>();
        var finalVerdicts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in log.Split(';'))
        {
            string[] parts = entry.Split('=');
            bool readable = parts.Length == 2 && parts[0].Length > 0
                && parts[1] is "ok" or "failed" or "skipped" or "canceled";
            if (!readable)
            {
                throw new ArgumentException("Une entrée du journal est illisible.", nameof(log));
            }

            if (!finalVerdicts.ContainsKey(parts[0]))
            {
                order.Add(parts[0]);
            }

            finalVerdicts[parts[0]] = parts[1];
        }

        // Un échec final l'emporte sur toute annulation : sans échec, la première annulation est la
        // cause du rouge ; avec, elle n'en est qu'une victime.
        foreach (string status in new[] { "failed", "canceled" })
        {
            foreach (string job in order)
            {
                if (finalVerdicts[job] == status)
                {
                    return job + "|" + status;
                }
            }
        }

        return "none";
    }
}
