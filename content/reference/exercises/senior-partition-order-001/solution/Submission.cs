using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string OrderingRisks(string routing)
    {
        if (string.IsNullOrWhiteSpace(routing))
        {
            throw new ArgumentException("Un journal de routage vide ne s'audite pas.", nameof(routing));
        }

        // La référence est la première partition vue : le verdict reste le même quel que soit le
        // fragment de journal reçu, contrairement à une comparaison de proche en proche.
        var firstPartition = new Dictionary<string, string>(StringComparer.Ordinal);
        var atRisk = new HashSet<string>(StringComparer.Ordinal);

        foreach (string assignment in routing.Split(';'))
        {
            string[] parts = assignment.Split(':');
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                throw new ArgumentException("Une affectation du journal est illisible.", nameof(routing));
            }

            // Une clé éclatée sur deux partitions est consommée par deux fils indépendants :
            // la garantie d'ordre s'arrête là, quel que soit le nombre de partitions visitées.
            if (!firstPartition.TryAdd(parts[0], parts[1]) && firstPartition[parts[0]] != parts[1])
            {
                atRisk.Add(parts[0]);
            }
        }

        return string.Join(',', atRisk.OrderBy(key => key, StringComparer.Ordinal));
    }
}
