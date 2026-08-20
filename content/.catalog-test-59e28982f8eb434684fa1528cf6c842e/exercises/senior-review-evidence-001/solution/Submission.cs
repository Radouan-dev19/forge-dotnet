using System;
using System.Collections.Generic;

public static class Submission
{
    public static string TriagedSeverities(string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new ArgumentException("Un descriptif vide ne se trie pas.", nameof(remarks));
        }

        var verdicts = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string remark in remarks.Split(';'))
        {
            string[] parts = remark.Split(':');
            bool readable = parts.Length == 3 && parts[0].Length > 0
                && parts[1] is "blocker" or "major" or "minor"
                && parts[2] is "reproduces" or "theoretical" or "preference";
            if (!readable)
            {
                throw new ArgumentException("Une remarque du descriptif est illisible.", nameof(remarks));
            }

            if (!seen.Add(parts[0]))
            {
                throw new ArgumentException("Un nom de remarque est répété.", nameof(remarks));
            }

            // La preuve corrige la revendication sans jamais la promouvoir : la modestie d'un
            // relecteur qui a vu le défaut de près a valeur d'information.
            string severity = parts[2] switch
            {
                "preference" => "minor",
                "theoretical" when parts[1] == "blocker" => "major",
                _ => parts[1],
            };

            verdicts.Add(parts[0] + "=" + severity);
        }

        return string.Join(';', verdicts);
    }
}
