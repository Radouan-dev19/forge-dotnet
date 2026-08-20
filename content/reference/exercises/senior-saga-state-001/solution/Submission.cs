using System;
using System.Collections.Generic;

public static class Submission
{
    public static string SagaVerdict(string log)
    {
        if (string.IsNullOrWhiteSpace(log))
        {
            throw new ArgumentException("Un journal vide ne décrit aucune saga.", nameof(log));
        }

        var completed = new List<string>();
        var compensated = new HashSet<string>(StringComparer.Ordinal);
        bool failed = false;

        foreach (string entry in log.Split(';'))
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2 || parts[0].Length == 0)
            {
                throw new ArgumentException("Un événement du journal est illisible.", nameof(log));
            }

            switch (parts[1])
            {
                case "ok":
                    if (completed.Contains(parts[0]))
                    {
                        throw new ArgumentException("Une étape est accomplie deux fois.", nameof(log));
                    }

                    completed.Add(parts[0]);
                    break;

                case "fail":
                    // La saga s'arrête au premier échec : un second raconte une autre exécution.
                    if (failed)
                    {
                        throw new ArgumentException("Le journal porte deux échecs.", nameof(log));
                    }

                    failed = true;
                    break;

                case "compensated":
                    // Compenser sans échec en amont, ou une étape jamais accomplie, n'est pas de la
                    // prudence : c'est une histoire impossible, à investiguer avant de qualifier.
                    if (!failed || !completed.Contains(parts[0]) || !compensated.Add(parts[0]))
                    {
                        throw new ArgumentException("Une compensation ne correspond à rien.", nameof(log));
                    }

                    break;

                default:
                    throw new ArgumentException("Une issue du journal est hors vocabulaire.", nameof(log));
            }
        }

        if (!failed)
        {
            return "completed";
        }

        // La reprise défait en ordre inverse : la dernière accomplie encore debout est la
        // prochaine action, c'est donc elle que le blocage nomme.
        for (int index = completed.Count - 1; index >= 0; index--)
        {
            if (!compensated.Contains(completed[index]))
            {
                return "stuck|" + completed[index];
            }
        }

        return "compensated";
    }
}
