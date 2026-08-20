using System;

public static class Submission
{
    public static bool IsWithinReplayWindow(int timestamp, int nowUnix, int toleranceSeconds)
    {
        // Une tolérance négative ne décrit aucune fenêtre. La tolérance nulle, elle,
        // est valide : elle n'accepte que l'instant exact.
        if (toleranceSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceSeconds));
        }

        // Écart absolu en 64 bits : la différence de deux instants éloignés
        // déborderait un int, et la fenêtre est symétrique autour du présent.
        long drift = Math.Abs((long)nowUnix - timestamp);

        // Borne inclusive : à un écart égal à la tolérance, l'envoi passe encore.
        return drift <= toleranceSeconds;
    }
}
