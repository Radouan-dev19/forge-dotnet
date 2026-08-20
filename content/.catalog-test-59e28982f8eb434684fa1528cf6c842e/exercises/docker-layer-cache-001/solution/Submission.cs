using System;

public static class Submission
{
    public static int RebuiltLayers(int totalSteps, int changedStep)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSteps);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(changedStep);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(changedStep, totalSteps);

        // L'étape modifiée est elle-même reconstruite : c'est le « plus un » que l'on oublie, et
        // qui décale tout le compte d'une unité.
        return totalSteps - changedStep + 1;
    }
}
