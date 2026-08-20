using System;
using System.Collections.Generic;

public static class Submission
{
    public static int OrphanSpans(string spans)
    {
        if (string.IsNullOrWhiteSpace(spans))
        {
            throw new ArgumentException("Un journal vide ne mesure aucune propagation.", nameof(spans));
        }

        string[] entries = spans.Split(';');
        var parentsBySpan = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in entries)
        {
            string[] parts = entry.Split('>');
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                throw new ArgumentException("Un segment du journal est illisible.", nameof(spans));
            }

            // Deux segments sous le même identifiant rendent chaque lien ambigu : le journal
            // lui-même est abîmé, ce qui n'est pas la même chose qu'une propagation abîmée.
            if (!parentsBySpan.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un identifiant de segment est répété.", nameof(spans));
            }
        }

        // Second passage : les liens ne se jugent qu'une fois tous les identifiants connus,
        // car un enfant arrive souvent au collecteur avant son parent.
        int orphans = 0;
        foreach (KeyValuePair<string, string> span in parentsBySpan)
        {
            if (span.Value == "-")
            {
                continue;
            }

            // Le segment auto-parent ne se raccroche à aucune cause : un cycle d'un nœud est orphelin.
            if (span.Value == span.Key || !parentsBySpan.ContainsKey(span.Value))
            {
                orphans++;
            }
        }

        return orphans;
    }
}
