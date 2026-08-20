using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string OrderDependentTests(string journal)
    {
        if (string.IsNullOrWhiteSpace(journal))
        {
            throw new ArgumentException("Un journal vide ne permet aucun diagnostic.", nameof(journal));
        }

        var verdictsByName = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        HashSet<string>? firstRunNames = null;

        foreach (string run in journal.Split('|'))
        {
            var runNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (string entry in run.Split(','))
            {
                string[] parts = entry.Split('=');
                bool readable = parts.Length == 2 && parts[0].Length > 0
                    && (parts[1] == "ok" || parts[1] == "ko");
                if (!readable)
                {
                    throw new ArgumentException("Une entrée du journal est illisible.", nameof(journal));
                }

                if (!runNames.Add(parts[0]))
                {
                    throw new ArgumentException("Un test apparaît deux fois dans la même exécution.", nameof(journal));
                }

                if (!verdictsByName.TryGetValue(parts[0], out HashSet<string>? verdicts))
                {
                    verdicts = new HashSet<string>(StringComparer.Ordinal);
                    verdictsByName.Add(parts[0], verdicts);
                }

                verdicts.Add(parts[1]);
            }

            // Un test absent d'une exécution n'a pas un verdict différent : il n'a pas de verdict.
            // Deux campagnes incomparables sont refusées plutôt que devinées.
            firstRunNames ??= runNames;
            if (!firstRunNames.SetEquals(runNames))
            {
                throw new ArgumentException("Les exécutions ne couvrent pas les mêmes tests.", nameof(journal));
            }
        }

        // Deux verdicts distincts pour le même code : seule la place dans la suite a changé.
        return string.Join(',', verdictsByName
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => pair.Key)
            .OrderBy(name => name, StringComparer.Ordinal));
    }
}
