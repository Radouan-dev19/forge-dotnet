using System;

public static class Submission
{
    public static int NextTokenLifetime(int absoluteExpiryUnix, int nowUnix, int slidingLifetimeSeconds)
    {
        // Composez les deux horloges : la fenêtre de glissement et le reste de session,
        // dont la plus courte l'emporte — et décidez du sort des deux bornes.
        throw new NotImplementedException("Le calcul de la fenêtre de rotation reste à écrire.");
    }
}
