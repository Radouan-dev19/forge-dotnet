using System;
using System.Collections.Generic;

public static class Submission
{
    public static string GateDecision(string checks, string required)
    {
        if (string.IsNullOrWhiteSpace(checks))
        {
            throw new ArgumentException("Un rapport vide ne permet aucune décision.", nameof(checks));
        }

        if (string.IsNullOrWhiteSpace(required))
        {
            throw new ArgumentException("Une porte sans exigences est mal configurée.", nameof(required));
        }

        var statuses = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in checks.Split(';'))
        {
            string[] parts = entry.Split('=');
            bool readable = parts.Length == 2 && parts[0].Length > 0
                && parts[1] is "ok" or "ko" or "pending";
            if (!readable)
            {
                throw new ArgumentException("Une entrée du rapport est illisible.", nameof(checks));
            }

            if (!statuses.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un contrôle est rapporté deux fois.", nameof(checks));
            }
        }

        string[] evidences = required.Split(',');
        foreach (string evidence in evidences)
        {
            if (evidence.Length == 0)
            {
                throw new ArgumentException("Une exigence de la porte est sans nom.", nameof(required));
            }

            // Un refus domine toute attente : le verdict certain part avant les incertitudes.
            if (statuses.TryGetValue(evidence, out string? status) && status == "ko")
            {
                return "refused|" + evidence;
            }
        }

        foreach (string evidence in evidences)
        {
            // Le silence vaut attente : une preuve jamais rapportée n'a pas échoué, et elle
            // n'absout pas non plus — la porte n'ouvre jamais quand son instrument se tait.
            if (!statuses.TryGetValue(evidence, out string? status) || status == "pending")
            {
                return "waiting|" + evidence;
            }
        }

        return "open";
    }
}
