using System;
using System.Collections.Generic;

public static class Submission
{
    // L'ordre canonique du point quotidien : où j'en suis, où je vais, ce qui m'arrête. Le blocage
    // vient en dernier parce qu'il appelle une réponse, et qu'on ne la demande pas avant d'avoir
    // situé son travail.
    private static readonly string[] CanonicalLabels = ["done", "next", "blocker"];

    public static string OrderStandup(string update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var byLabel = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (string label in CanonicalLabels)
        {
            byLabel[label] = [];
        }

        foreach (string rawLine in update.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Trim();
            int separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            string label = line[..separator].Trim().ToLowerInvariant();
            string text = line[(separator + 1)..].Trim();
            if (text.Length == 0 || !byLabel.TryGetValue(label, out List<string>? entries))
            {
                continue;
            }

            // L'ajout en fin de liste préserve l'ordre d'arrivée à étiquette égale : c'est une
            // chronologie, la trier la détruirait.
            entries.Add($"{label}: {text}");
        }

        var ordered = new List<string>();
        foreach (string label in CanonicalLabels)
        {
            ordered.AddRange(byLabel[label]);
        }

        return string.Join("\n", ordered);
    }
}
