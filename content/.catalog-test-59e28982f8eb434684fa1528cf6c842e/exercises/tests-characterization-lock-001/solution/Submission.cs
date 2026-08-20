using System;

public static class Submission
{
    public static decimal LegacyShippingFee(decimal subtotal, int items)
    {
        if (items < 1)
        {
            throw new ArgumentException("Le système historique refuse un panier sans article.", nameof(items));
        }

        if (subtotal < 0m)
        {
            throw new ArgumentException("Le système historique refuse un sous-total négatif.", nameof(subtotal));
        }

        // La gratuité observée ne s'applique qu'au-delà de cent, jamais à cent tout rond : la
        // caractérisation fige ce comportement tel quel, elle ne le corrige pas.
        if (subtotal > 100m)
        {
            return 0.00m;
        }

        // Seul l'excédent au-delà du cinquième article est facturé, et jamais en négatif.
        int surchargedItems = Math.Max(0, items - 5);

        // Littéraux à deux décimales : le montant garde l'échelle que la facturation affiche.
        return 4.90m + 0.50m * surchargedItems;
    }
}
