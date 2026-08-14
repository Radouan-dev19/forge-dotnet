public static class Submission
{
    public static decimal PaymentFee(decimal amount, bool isCard)
    {
        // Un montant négatif ne décrit aucun paiement : refus avant tout calcul.
        if (amount < 0m)
        {
            throw new System.ArgumentOutOfRangeException(nameof(amount));
        }

        // Deux politiques de frais : la carte porte un taux, l'autre moyen est gratuit.
        decimal rate = isCard ? 0.015m : 0m;

        // Arrondi commercial au centime, une seule fois, après le calcul.
        return decimal.Round(amount * rate, 2, System.MidpointRounding.AwayFromZero);
    }
}
