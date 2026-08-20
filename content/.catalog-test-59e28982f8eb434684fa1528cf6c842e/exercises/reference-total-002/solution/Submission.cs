public static class Submission
{
    public static decimal ApplyDiscount(decimal total, decimal rate)
    {
        // Deux invariants : total positif ou nul, taux dans [0, 1]. Hors domaine, refus.
        if (total < 0m || rate < 0m || rate > 1m)
        {
            throw new System.ArgumentOutOfRangeException();
        }

        // Le net se calcule en pleine précision, puis s'arrondit une seule fois au centime.
        return decimal.Round(total * (1m - rate), 2, System.MidpointRounding.AwayFromZero);
    }
}
