public static class Submission
{
    public static decimal NetTotal(decimal total, bool isVip)
    {
        // Aucun total négatif n'a de sens ici : la validation précède la politique.
        if (total < 0m)
        {
            throw new System.ArgumentOutOfRangeException(nameof(total));
        }

        // La politique choisit un taux ; le calcul du net est le même pour tous.
        decimal rate = isVip ? 0.10m : 0m;

        // Un seul arrondi, au centime, en sortie : le point métier annoncé.
        return decimal.Round(total * (1m - rate), 2, System.MidpointRounding.AwayFromZero);
    }
}
