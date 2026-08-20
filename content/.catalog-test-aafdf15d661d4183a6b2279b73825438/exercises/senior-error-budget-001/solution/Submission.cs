using System;

public static class Submission
{
    public static string BudgetDecision(int totalRequests, int failedRequests, int sloBasisPoints)
    {
        // Une fenetre sans requete n'a rien a evaluer.
        if (totalRequests < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalRequests), "Le total de requetes doit etre positif.");
        }

        // Des echecs hors de zero..total signalent un bug de collecte, pas une decision.
        if (failedRequests < 0 || failedRequests > totalRequests)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedRequests), "Les echecs doivent tenir entre zero et le total.");
        }

        // Le SLO est un taux de succes en points de base : de 0 a 10000.
        if (sloBasisPoints < 0 || sloBasisPoints > 10000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sloBasisPoints), "Le SLO doit etre exprime entre 0 et 10000 points de base.");
        }

        // Tolerance = complement du SLO ; budget = tolerance appliquee au volume.
        // On elargit le produit pour eviter tout debordement d'entier sur de gros volumes.
        long toleranceBasisPoints = 10000L - sloBasisPoints;
        long allowedFailures = (long)totalRequests * toleranceBasisPoints / 10000L;

        // Le budget epuise gele les livraisons ; l'egalite reste livrable.
        return failedRequests > allowedFailures ? "freeze" : "ship";
    }
}
