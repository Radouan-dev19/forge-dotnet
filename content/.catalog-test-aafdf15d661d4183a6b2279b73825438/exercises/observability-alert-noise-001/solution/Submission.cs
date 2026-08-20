using System;

public static class Submission
{
    public static int FirstAlertIndex(int[] errorRates, int threshold, int requiredStreak)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(requiredStreak, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);

        // Une fenêtre corrompue se refuse en entier, avant toute décision : une mesure négative
        // après le déclenchement invaliderait aussi ce qui a été décidé.
        foreach (int sample in errorRates)
        {
            if (sample < 0)
            {
                throw new ArgumentException("Un taux d'erreur négatif est une mesure corrompue.", nameof(errorRates));
            }
        }

        int streak = 0;
        for (int index = 0; index < errorRates.Length; index++)
        {
            // Le seuil exact compte — au niveau ou au-dessus — et l'accalmie remet tout à zéro :
            // c'est la remise à zéro stricte qui donne son sens au mot consécutif.
            streak = errorRates[index] >= threshold ? streak + 1 : 0;

            // L'alerte part à l'échantillon qui complète la série, pas à celui qui la commence.
            if (streak == requiredStreak)
            {
                return index;
            }
        }

        // Fenêtre épuisée sans série complète : du bruit, pas une panne. La fenêtre vide n'alerte pas.
        return -1;
    }
}
