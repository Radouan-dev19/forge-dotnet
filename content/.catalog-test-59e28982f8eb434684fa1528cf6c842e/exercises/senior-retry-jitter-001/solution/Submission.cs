using System;

public static class Submission
{
    public static int[] RetrySchedule(int attempts, int baseDelayMs, int capMs, int seed)
    {
        // Au-delà de dix tentatives, la relance masque une panne ; au-delà de la minute, l'attente
        // est un report de traitement qui relève d'une file, pas d'une politique de relance.
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attempts, 10);
        ArgumentOutOfRangeException.ThrowIfLessThan(baseDelayMs, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(capMs, baseDelayMs);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capMs, 60_000);
        ArgumentOutOfRangeException.ThrowIfNegative(seed);

        var schedule = new int[attempts - 1];
        int window = baseDelayMs;

        for (int rank = 1; rank < attempts; rank++)
        {
            int half = window / 2;

            // Produit en entier large : une grande graine légitime déborderait le trente-deux bits,
            // et un modulo sur un débordement rendrait des attentes sous le plancher, en silence.
            int jitter = (int)((long)seed * rank % (half + 1));

            // Jitter égal : l'attente vit dans la moitié haute de la fenêtre — le plancher garantit
            // le repos du serveur, la moitié haute porte la désynchronisation entre clients.
            schedule[rank - 1] = half + jitter;

            // La fenêtre se replafonne à chaque doublement : écrêter une seule fois ne suffit pas.
            window = window > capMs / 2 ? capMs : window * 2;
        }

        return schedule;
    }
}
