using System;

public static class Submission
{
    // Suit le nombre d'abonnements RxJS restes actifs apres une sequence d'evenements du composant.
    public static int ActiveSubscriptions(string events)
    {
        int active = 0;
        foreach (string token in Split(events))
        {
            if (token == "open")
            {
                active++;
            }
            else if (token == "close" && active > 0)
            {
                active--;
            }
            else if (token == "navigate")
            {
                // Evenement de changement d'ecran du composant.
            }
        }

        return active;
    }

    private static string[] Split(string events) =>
        events.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
