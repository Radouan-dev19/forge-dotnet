using System;

public static class Submission
{
    public static int NextTokenLifetime(int absoluteExpiryUnix, int nowUnix, int slidingLifetimeSeconds)
    {
        // Un glissement non positif ne décrit aucune politique : faute d'appel.
        if (slidingLifetimeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slidingLifetimeSeconds));
        }

        // Session finie : fenêtre nulle — un état ordinaire du cycle de vie.
        if (nowUnix >= absoluteExpiryUnix)
        {
            return 0;
        }

        // Reste de session en 64 bits : l'écart de deux instants peut déborder un int.
        long remaining = (long)absoluteExpiryUnix - nowUnix;

        // La fenêtre normale, rabattue par le plafond absolu en fin de session.
        return (int)Math.Min(slidingLifetimeSeconds, remaining);
    }
}
