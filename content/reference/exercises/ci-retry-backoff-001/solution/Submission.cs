using System;

public static class Submission
{
    public static int RetryWindowSeconds(int attempts, int baseDelaySeconds, int capSeconds)
    {
        // Chaque borne encode un jugement d'exploitation : au-delà de cent tentatives, la relance
        // masque une panne ; au-delà d'une journée de plafond, l'attente est un abandon.
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attempts, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(baseDelaySeconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(capSeconds, baseDelaySeconds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capSeconds, 86_400);

        int window = 0;
        int delay = baseDelaySeconds;

        // n tentatives, n moins une attentes : rien avant la première, rien après la dernière.
        for (int gap = 1; gap < attempts; gap++)
        {
            window += delay;

            // Comparer avant de doubler : la valeur courante reste sous le plafond, donc son
            // double reste calculable, et l'écrêtage borne toute la suite.
            delay = delay > capSeconds / 2 ? capSeconds : delay * 2;
        }

        return window;
    }
}
