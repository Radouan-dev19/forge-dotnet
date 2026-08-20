using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public static class Submission
{
    private static readonly string[] Levels = ["INFO", "WARN", "ERROR"];

    public static int CountBySeverity(string logs, string severity)
    {
        ArgumentNullException.ThrowIfNull(logs);
        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Le niveau demandé est obligatoire.", nameof(severity));
        }

        string wanted = severity.Trim();
        int count = 0;
        foreach (Entry entry in ReadEntries(logs))
        {
            if (string.Equals(entry.Level, wanted, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    public static Dictionary<string, int> GroupByMessage(string logs)
    {
        ArgumentNullException.ThrowIfNull(logs);
        var groups = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Entry entry in ReadEntries(logs))
        {
            if (!string.Equals(entry.Level, "ERROR", StringComparison.Ordinal))
            {
                continue;
            }

            string key = NormalizeMessage(entry.Message);
            groups[key] = groups.TryGetValue(key, out int current) ? current + 1 : 1;
        }

        return groups;
    }

    public static string ErrorReport(string logs)
    {
        ArgumentNullException.ThrowIfNull(logs);
        return string.Join(
            "\n",
            GroupByMessage(logs)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Value.ToString(CultureInfo.InvariantCulture) + " x " + pair.Key));
    }

    // Une suite de chiffres, quelle que soit sa longueur, devient un seul marqueur : c'est ce qui
    // réunit deux incidents qui ne diffèrent que par un identifiant.
    private static string NormalizeMessage(string message)
    {
        var builder = new StringBuilder(message.Length);
        bool inDigits = false;
        foreach (char character in message)
        {
            if (char.IsAsciiDigit(character))
            {
                if (!inDigits)
                {
                    builder.Append('#');
                    inDigits = true;
                }

                continue;
            }

            inDigits = false;
            builder.Append(character);
        }

        return builder.ToString();
    }

    private static List<Entry> ReadEntries(string logs)
    {
        var entries = new List<Entry>();
        foreach (string rawLine in logs.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !Levels.Contains(parts[1], StringComparer.Ordinal))
            {
                continue;
            }

            entries.Add(new Entry(parts[1], parts[2].Trim()));
        }

        return entries;
    }

    private sealed record Entry(string Level, string Message);
}
