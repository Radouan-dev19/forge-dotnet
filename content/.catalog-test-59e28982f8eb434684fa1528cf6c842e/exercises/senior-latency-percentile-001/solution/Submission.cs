using System;

public static class Submission
{
    public static int Percentile(int[] latencies, int percentile)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(percentile, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentile, 100);

        if (latencies.Length == 0)
        {
            throw new ArgumentException("Un percentile sans mesure n'existe pas.", nameof(latencies));
        }

        // La copie est un contrat : trier en place corromprait la fenêtre de mesures de l'appelant.
        var sorted = new int[latencies.Length];
        for (int index = 0; index < latencies.Length; index++)
        {
            if (latencies[index] < 0)
            {
                throw new ArgumentException("Une latence négative ne mesure rien.", nameof(latencies));
            }

            sorted[index] = latencies[index];
        }

        Array.Sort(sorted);

        // Rang au plafond, indexé depuis un : au moins la part demandée des mesures est couverte,
        // et la valeur rendue est une latence réellement vécue — jamais une interpolation.
        int rank = (percentile * sorted.Length + 99) / 100;
        return sorted[rank - 1];
    }
}
