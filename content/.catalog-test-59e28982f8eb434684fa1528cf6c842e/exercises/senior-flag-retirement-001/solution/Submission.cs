using System;
using System.Collections.Generic;

public static class Submission
{
    public static string FlagRetirements(string flags, int minAgeDays)
    {
        // Le seuil d'âge est le délai de rétractation de l'équipe : nul, il n'en est plus un.
        ArgumentOutOfRangeException.ThrowIfLessThan(minAgeDays, 1);

        if (string.IsNullOrWhiteSpace(flags))
        {
            throw new ArgumentException("Un registre vide ne s'audite pas.", nameof(flags));
        }

        var retirements = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string flag in flags.Split(';'))
        {
            string[] parts = flag.Split(':');
            bool readable = parts.Length == 3 && parts[0].Length > 0
                && parts[1] is "on-for-all" or "off-for-all" or "mixed"
                && int.TryParse(parts[2], out int age) && age >= 0;
            if (!readable)
            {
                throw new ArgumentException("Un drapeau du registre est illisible.", nameof(flags));
            }

            if (!seen.Add(parts[0]))
            {
                throw new ArgumentException("Un drapeau est décrit deux fois.", nameof(flags));
            }

            // Le mixte pilote encore — l'interrupteur d'urgence a légitimement des années — et
            // l'issue trop récente garde son droit à l'inversion : aucun des deux ne se retire.
            if (parts[1] == "mixed" || int.Parse(parts[2]) < minAgeDays)
            {
                continue;
            }

            // Deux sorts opposés : le gagnant intègre sa voie vivante, le perdant supprime sa
            // branche morte — les confondre détruit du code vivant ou garde du mort.
            retirements.Add(parts[0] + (parts[1] == "on-for-all" ? "=inline" : "=delete"));
        }

        return string.Join(';', retirements);
    }
}
