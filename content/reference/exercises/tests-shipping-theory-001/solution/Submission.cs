public static class Submission
{
    public static decimal ShippingCost(decimal total, bool express)
    {
        if (total < 0m)
        {
            throw new System.ArgumentOutOfRangeException(nameof(total));
        }

        // L'express prime : son tarif est fixe, quel que soit le total.
        if (express)
        {
            return 9.90m;
        }

        // Mode normal : gratuité à partir du seuil, borne incluse.
        return total >= 50m ? 0m : 4.90m;
    }
}
