public static class Submission
{
    public static decimal DiscountRate(decimal total)
    {
        if (total < 0m)
        {
            throw new System.ArgumentOutOfRangeException(nameof(total));
        }

        // Gardes ordonnées du palier le plus haut vers le plus bas : chaque seuil
        // est inclus — à deux cents exactement, le taux plein s'applique.
        if (total >= 200m)
        {
            return 0.15m;
        }

        if (total >= 100m)
        {
            return 0.05m;
        }

        return 0m;
    }
}
