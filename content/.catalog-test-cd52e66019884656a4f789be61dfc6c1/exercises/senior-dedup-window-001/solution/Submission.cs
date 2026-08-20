using System;
using System.Collections.Generic;

public static class Submission
{
    public static int MissedDuplicates(string deliveries, int windowMinutes)
    {
        // Une fenêtre nulle n'est pas une politique : c'est l'absence de déduplication.
        ArgumentOutOfRangeException.ThrowIfLessThan(windowMinutes, 1);

        if (string.IsNullOrWhiteSpace(deliveries))
        {
            throw new ArgumentException("Un journal vide ne mesure aucun risque.", nameof(deliveries));
        }

        var lastSeen = new Dictionary<string, int>(StringComparer.Ordinal);
        int previousMinute = -1;
        int missed = 0;

        foreach (string entry in deliveries.Split(';'))
        {
            string[] parts = entry.Split(':');
            bool readable = parts.Length == 2 && parts[1].Length > 0
                && int.TryParse(parts[0], out int minute) && minute >= 0;
            if (!readable)
            {
                throw new ArgumentException("Une livraison du journal est illisible.", nameof(deliveries));
            }

            int current = int.Parse(parts[0]);
            if (current < previousMinute)
            {
                throw new ArgumentException("La chronologie des livraisons recule.", nameof(deliveries));
            }

            previousMinute = current;

            // La fenêtre glisse depuis la dernière livraison : au-delà strict, l'identifiant a été
            // oublié et le doublon est réappliqué à tort.
            if (lastSeen.TryGetValue(parts[1], out int seenAt) && current - seenAt > windowMinutes)
            {
                missed++;
            }

            // Le magasin enregistre ce qu'il traite, sans savoir que c'était un doublon : la
            // mémoire se rafraîchit même quand la livraison a échappé à la fenêtre.
            lastSeen[parts[1]] = current;
        }

        return missed;
    }
}
