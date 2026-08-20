using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string ExtractionOrder(string modules)
    {
        if (string.IsNullOrWhiteSpace(modules))
        {
            throw new ArgumentException("Une description vide ne planifie rien.", nameof(modules));
        }

        var described = new Dictionary<string, (int Inbound, int Outbound)>(StringComparer.Ordinal);
        foreach (string module in modules.Split(';'))
        {
            string[] parts = module.Split(':');
            bool readable = parts.Length == 3 && parts[0].Length > 0
                && int.TryParse(parts[1], out int inbound) && inbound >= 0
                && int.TryParse(parts[2], out int outbound) && outbound >= 0;
            if (!readable)
            {
                throw new ArgumentException("Un module de la description est illisible.", nameof(modules));
            }

            if (!described.TryAdd(parts[0], (int.Parse(parts[1]), int.Parse(parts[2]))))
            {
                throw new ArgumentException("Un module est décrit deux fois.", nameof(modules));
            }
        }

        // Les entrantes ordonnent — chaque appelant est à repointer le jour de la bascule — et les
        // sortantes départagent : le module autonome vit mieux une fois seul. Le nom fige le reste.
        var plan = described
            .OrderBy(pair => pair.Value.Inbound)
            .ThenBy(pair => pair.Value.Outbound)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Key);

        return string.Join(';', plan);
    }
}
