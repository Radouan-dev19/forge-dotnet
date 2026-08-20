using System;

public static class Submission
{
    public static bool IsWithinReplayWindow(int timestamp, int nowUnix, int toleranceSeconds)
    {
        // Comparez l'écart absolu entre l'instant courant et l'horodatage à la tolérance,
        // fenêtre symétrique pour absorber la dérive d'horloge dans les deux sens.
        throw new NotImplementedException("Le contrôle de fenêtre de rejeu reste à écrire.");
    }
}
