using System;
using System.Collections.Generic;

public static class Submission
{
    // Référentiel ordonné : gravité décroissante puis ordre fixe — la sortie hérite de cet ordre.
    private static readonly (string Setting, string Hardened, string Gap, string Severity)[] Baseline =
    [
        ("user", "app", "root-user", "critical"),
        ("escalation", "blocked", "privilege-escalation", "critical"),
        ("network", "bridge", "host-network", "high"),
        ("capabilities", "dropped", "default-capabilities", "high"),
        ("filesystem", "read-only", "writable-filesystem", "medium"),
    ];

    private static readonly Dictionary<string, string[]> Vocabulary = new(StringComparer.Ordinal)
    {
        ["user"] = ["app", "root"],
        ["escalation"] = ["blocked", "allowed"],
        ["network"] = ["bridge", "host"],
        ["capabilities"] = ["dropped", "default"],
        ["filesystem"] = ["read-only", "writable"],
    };

    public static string HardeningGaps(string configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            throw new ArgumentException("Une configuration vide ne s'audite pas.", nameof(configuration));
        }

        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in configuration.Split(';'))
        {
            string[] parts = pair.Split('=');
            bool readable = parts.Length == 2
                && Vocabulary.TryGetValue(parts[0], out string[]? allowed)
                && Array.IndexOf(allowed, parts[1]) >= 0;
            if (!readable)
            {
                throw new ArgumentException("Un réglage de la configuration est illisible.", nameof(configuration));
            }

            if (!settings.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un réglage est décrit deux fois.", nameof(configuration));
            }
        }

        // Ce que l'audit n'a pas vu, il ne le certifie pas : l'absence vaut refus, jamais conformité.
        if (settings.Count != Baseline.Length)
        {
            throw new ArgumentException("Un réglage obligatoire manque à la configuration.", nameof(configuration));
        }

        var gaps = new List<string>();
        foreach ((string setting, string hardened, string gap, string severity) in Baseline)
        {
            if (settings[setting] != hardened)
            {
                gaps.Add(gap + "=" + severity);
            }
        }

        return gaps.Count == 0 ? "compliant" : string.Join(';', gaps);
    }
}
