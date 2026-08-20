using System;

public static class Submission
{
    public static int RemainingErrorBudget(int totalRequests, int failedRequests, decimal objectivePercent)
    {
        if (totalRequests < 0 || failedRequests < 0 || failedRequests > totalRequests)
        {
            throw new ArgumentException("La fenêtre décrite n'existe pas.", nameof(failedRequests));
        }

        // Cent tout rond reste accepté : un budget nul est exigeant mais cohérent.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(objectivePercent, 0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(objectivePercent, 100m);

        // Part tolérée en décimal exact, puis plancher : arrondir au plus proche offrirait un
        // échec que l'objectif ne concède pas, et le flottant binaire déplacerait le plancher.
        decimal toleratedShare = (100m - objectivePercent) / 100m;
        int allowed = (int)Math.Floor(totalRequests * toleratedShare);

        // Le dépassement se chiffre : un restant négatif est la moitié utile du signal.
        return allowed - failedRequests;
    }
}
