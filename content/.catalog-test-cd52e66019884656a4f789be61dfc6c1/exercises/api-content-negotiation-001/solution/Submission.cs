using System;
using System.Collections.Generic;
using System.Globalization;

public static class Submission
{
    public static string SelectMediaType(string accepted, string supported)
    {
        ArgumentNullException.ThrowIfNull(accepted);
        ArgumentNullException.ThrowIfNull(supported);

        List<string> serverTypes = SplitList(supported);
        if (serverTypes.Count == 0)
        {
            return string.Empty;
        }

        List<Preference> client = ReadPreferences(accepted);
        if (client.Count == 0)
        {
            // Aucune préférence exprimée : tout convient, et le serveur garde la main.
            return serverTypes[0];
        }

        string best = string.Empty;
        decimal bestQuality = 0m;
        foreach (string type in serverTypes)
        {
            decimal quality = QualityOf(client, type);

            // Comparaison strictement supérieure : à qualité égale, le type déjà retenu — donc le
            // plus haut dans la préférence du serveur — conserve la main.
            if (quality > bestQuality)
            {
                best = type;
                bestQuality = quality;
            }
        }

        return best;
    }

    /// <summary>
    /// Qualité que le client accorde à un type : son entrée exacte si elle existe, sinon celle du
    /// passe-partout. Une entrée exacte à zéro est un refus, que le passe-partout ne rattrape pas.
    /// </summary>
    private static decimal QualityOf(List<Preference> client, string type)
    {
        decimal wildcard = 0m;
        bool hasWildcard = false;
        foreach (Preference preference in client)
        {
            if (string.Equals(preference.Type, type, StringComparison.OrdinalIgnoreCase))
            {
                return preference.Quality;
            }

            if (string.Equals(preference.Type, "*/*", StringComparison.Ordinal))
            {
                wildcard = preference.Quality;
                hasWildcard = true;
            }
        }

        return hasWildcard ? wildcard : 0m;
    }

    private static List<Preference> ReadPreferences(string accepted)
    {
        var preferences = new List<Preference>();
        foreach (string entry in SplitList(accepted))
        {
            string[] parts = entry.Split(';');
            string type = parts[0].Trim();
            if (type.Length == 0)
            {
                continue;
            }

            decimal quality = 1m;
            for (int index = 1; index < parts.Length; index++)
            {
                string parameter = parts[index].Trim();
                if (parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && decimal.TryParse(
                        parameter[2..],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal parsed))
                {
                    quality = parsed;
                }
            }

            preferences.Add(new Preference(type, quality));
        }

        return preferences;
    }

    private static List<string> SplitList(string value)
    {
        var items = new List<string>();
        foreach (string part in value.Split(','))
        {
            string item = part.Trim();
            if (item.Length > 0)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private readonly record struct Preference(string Type, decimal Quality);
}
