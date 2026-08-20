using System;
using System.Collections.Generic;

public static class Submission
{
    public static string FirstLeakingTest(string trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        // État de la base partagée : il survit d'un test au suivant, et c'est tout le problème.
        var store = new HashSet<string>(StringComparer.Ordinal);

        foreach (string rawTest in trace.Split('|'))
        {
            string segment = rawTest.Trim();
            if (segment.Length == 0)
            {
                continue;
            }

            int separator = segment.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                throw new ArgumentException("Un segment de trace ne nomme pas son test.", nameof(trace));
            }

            string name = segment[..separator].Trim();

            // Clés insérées par ce test pendant son propre passage : elles seules rendent une
            // lecture légitime, indépendamment de ce que la base contenait déjà.
            var inserted = new HashSet<string>(StringComparer.Ordinal);
            bool leaks = false;

            foreach (string rawOperation in segment[(separator + 1)..].Split(','))
            {
                string operation = rawOperation.Trim();
                if (operation.Length < 2)
                {
                    throw new ArgumentException("Une opération de trace est incomplète.", nameof(trace));
                }

                string key = operation[1..].Trim();
                switch (operation[0])
                {
                    case '+':
                        store.Add(key);
                        inserted.Add(key);
                        break;
                    case '-':
                        store.Remove(key);
                        inserted.Remove(key);
                        break;
                    case '?':
                        // La lecture d'une clé absente n'est pas une fuite : le test échouera, ce qui
                        // est visible. La fuite, elle, produit un test vert qui ne prouve rien.
                        if (store.Contains(key) && !inserted.Contains(key))
                        {
                            leaks = true;
                        }

                        break;
                    default:
                        throw new ArgumentException("Une opération de trace est inconnue.", nameof(trace));
                }
            }

            // Le premier test qui fuit suffit : les suivants sont souvent des conséquences du même
            // défaut de nettoyage, et les énumérer masquerait la cause.
            if (leaks)
            {
                return name;
            }
        }

        return string.Empty;
    }
}
