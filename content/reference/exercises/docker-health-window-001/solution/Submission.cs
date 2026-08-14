public static class Submission
{
    public static bool FitsHealthBudget(int intervalSeconds, int retries, int budgetSeconds)
    {
        // Des durées ou des essais non positifs ne décrivent aucune sonde : refus.
        if (intervalSeconds <= 0 || retries <= 0 || budgetSeconds <= 0)
        {
            return false;
        }

        // La fenêtre totale — intervalle fois essais — doit tenir dans le budget,
        // borne incluse. Le produit est vérifié contre le débordement.
        return checked(intervalSeconds * retries) <= budgetSeconds;
    }
}
