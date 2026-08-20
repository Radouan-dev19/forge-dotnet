using System;
using System.Collections.Generic;

public static class Submission
{
    private static readonly Dictionary<string, string[]> RequiredByState = new(StringComparer.Ordinal)
    {
        ["closed"] = ["calls", "failures", "minimum-calls", "max-rate"],
        ["open"] = ["elapsed", "cooldown"],
        ["half-open"] = ["probes", "probe-failures", "required-probes"],
    };

    public static string BreakerDecision(string window)
    {
        if (string.IsNullOrWhiteSpace(window))
        {
            throw new ArgumentException("Un relevé vide ne décide d'aucune transition.", nameof(window));
        }

        var measures = new Dictionary<string, int>(StringComparer.Ordinal);
        string? state = null;
        foreach (string pair in window.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Une mesure du relevé est illisible.", nameof(window));
            }

            if (parts[0] == "state")
            {
                if (state is not null || !RequiredByState.ContainsKey(parts[1]))
                {
                    throw new ArgumentException("L'état du relevé est répété ou inconnu.", nameof(window));
                }

                state = parts[1];
                continue;
            }

            if (!int.TryParse(parts[1], out int value) || value < 0 || !measures.TryAdd(parts[0], value))
            {
                throw new ArgumentException("Une mesure est non numérique, négative ou répétée.", nameof(window));
            }
        }

        // Chaque état exige exactement ses mesures : un relevé qui mélange deux états ne décrit
        // aucun instant réel du disjoncteur.
        if (state is null)
        {
            throw new ArgumentException("Le relevé ne déclare pas d'état.", nameof(window));
        }

        string[] required = RequiredByState[state];
        if (measures.Count != required.Length)
        {
            throw new ArgumentException("Le relevé ne porte pas exactement les mesures de son état.", nameof(window));
        }

        foreach (string key in required)
        {
            if (!measures.ContainsKey(key))
            {
                throw new ArgumentException("Une mesure exigée par l'état est absente.", nameof(window));
            }
        }

        return state switch
        {
            "closed" => DecideClosed(measures),
            "open" => measures["elapsed"] >= measures["cooldown"] ? "half-open|probe-allowed" : "stay-open|cooling",
            _ => DecideHalfOpen(measures),
        };
    }

    private static string DecideClosed(Dictionary<string, int> measures)
    {
        if (measures["failures"] > measures["calls"] || measures["max-rate"] > 100)
        {
            throw new ArgumentException("La fenêtre fermée est incohérente.", nameof(measures));
        }

        // La garde de volume précède le taux : une fraction sur trois appels ne prouve rien,
        // et un disjoncteur nerveux coupe des services sains à chaque creux de trafic.
        if (measures["calls"] < measures["minimum-calls"])
        {
            return "stay-closed|insufficient-data";
        }

        // Produits croisés en long, inégalité stricte : pas d'arrondi flottant qui décide, pas de
        // débordement sur une fenêtre de deux milliards d'appels, et le taux exact reste toléré.
        if (measures["failures"] * 100L > (long)measures["max-rate"] * measures["calls"])
        {
            return "open|rate-exceeded";
        }

        return "stay-closed|healthy";
    }

    private static string DecideHalfOpen(Dictionary<string, int> measures)
    {
        if (measures["probe-failures"] > measures["probes"] || measures["required-probes"] < 1)
        {
            throw new ArgumentException("Le relevé de sondes est incohérent.", nameof(measures));
        }

        // Une sonde en échec répond non : fermer malgré elle métronomerait la charge sur un
        // service qui vient d'échouer.
        if (measures["probe-failures"] > 0)
        {
            return "open|probe-failed";
        }

        return measures["probes"] >= measures["required-probes"]
            ? "closed|probes-passed"
            : "stay-half-open|probing";
    }
}
