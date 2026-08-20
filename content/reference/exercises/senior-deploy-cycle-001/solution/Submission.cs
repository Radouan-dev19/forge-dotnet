using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string DeployCycles(string edges)
    {
        if (string.IsNullOrWhiteSpace(edges))
        {
            throw new ArgumentException("Un graphe vide n'a pas de cycle à relever.", nameof(edges));
        }

        var successors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var seenEdges = new HashSet<string>(StringComparer.Ordinal);
        foreach (string edge in edges.Split(';'))
        {
            string[] parts = edge.Split('>');
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                throw new ArgumentException("Une arête du graphe est illisible.", nameof(edges));
            }

            if (!seenEdges.Add(edge))
            {
                throw new ArgumentException("Une arête du graphe est répétée.", nameof(edges));
            }

            if (!successors.TryGetValue(parts[0], out HashSet<string>? targets))
            {
                targets = new HashSet<string>(StringComparer.Ordinal);
                successors.Add(parts[0], targets);
            }

            targets.Add(parts[1]);
        }

        // Un service est en cycle si un chemin part de ses appels sortants et revient à lui :
        // l'atteignabilité depuis les successeurs répond service par service.
        var inCycle = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string service in successors.Keys)
        {
            if (CanReach(successors, service, service))
            {
                inCycle.Add(service);
            }
        }

        return string.Join(',', inCycle);
    }

    private static bool CanReach(Dictionary<string, HashSet<string>> successors, string from, string target)
    {
        // L'ensemble des visités empêche de tourner dans le cycle qu'on est en train de chercher.
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var frontier = new Stack<string>();
        frontier.Push(from);

        while (frontier.Count > 0)
        {
            string current = frontier.Pop();
            if (!successors.TryGetValue(current, out HashSet<string>? nexts))
            {
                continue;
            }

            foreach (string next in nexts)
            {
                if (next == target)
                {
                    return true;
                }

                if (visited.Add(next))
                {
                    frontier.Push(next);
                }
            }
        }

        return false;
    }
}
