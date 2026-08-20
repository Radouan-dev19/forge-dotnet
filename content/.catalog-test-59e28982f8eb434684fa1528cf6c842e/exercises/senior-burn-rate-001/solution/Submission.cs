using System;

public static class Submission
{
    public static decimal BurnRate(int totalRequests, int failedRequests, decimal objectivePercent)
    {
        // Un taux d'échec sur zéro requête n'existe pas ; l'objectif parfait ne tolère rien et sa
        // vitesse serait une division par zéro — ce cas se gouverne au budget restant.
        ArgumentOutOfRangeException.ThrowIfLessThan(totalRequests, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(objectivePercent, 0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(objectivePercent, 100m);

        if (failedRequests < 0 || failedRequests > totalRequests)
        {
            throw new ArgumentException("La fenêtre décrite n'existe pas.", nameof(failedRequests));
        }

        // Deux taux en décimal exact : l'observé sur la fenêtre, le toléré depuis l'objectif.
        decimal observed = (decimal)failedRequests / totalRequests;
        decimal tolerated = (100m - objectivePercent) / 100m;

        // Plancher au centième : un chiffre d'alerte ne se flatte pas, surtout autour du seuil un.
        return Math.Floor(observed / tolerated * 100m) / 100m;
    }
}
