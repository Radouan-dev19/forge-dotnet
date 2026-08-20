using System;
using System.Collections.Generic;

public static class Submission
{
    public static string DoublesFor(string dependencies)
    {
        if (string.IsNullOrWhiteSpace(dependencies))
        {
            throw new ArgumentException("Un inventaire vide n'attribue aucun double.", nameof(dependencies));
        }

        var assignments = new List<string>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entry in dependencies.Split(';'))
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 3 || parts[0].Length == 0)
            {
                throw new ArgumentException("Une dépendance du descriptif est illisible.", nameof(dependencies));
            }

            // Deux doubles pour la même dépendance ne cohabitent pas dans un test.
            if (!seenNames.Add(parts[0]))
            {
                throw new ArgumentException("Une dépendance est décrite deux fois.", nameof(dependencies));
            }

            // Les deux croisements absents de la table sont les deux pathologies classiques :
            // espionner une lecture sur-spécifie, bouchonner une écriture ne vérifie rien.
            string kind = (parts[1], parts[2]) switch
            {
                ("incoming", "canned") => "stub",
                ("incoming", "behavioural") => "fake",
                ("outgoing", "state") => "fake",
                ("outgoing", "protocol") => "spy",
                _ => throw new ArgumentException(
                    "Un croisement flux-contrat du descriptif est incohérent.", nameof(dependencies)),
            };

            assignments.Add(parts[0] + "=" + kind);
        }

        return string.Join(';', assignments);
    }
}
