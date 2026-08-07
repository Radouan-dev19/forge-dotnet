public static class Submission
{
    public static bool FitsHealthBudget(int intervalSeconds, int retries, int budgetSeconds)
    {
        if (intervalSeconds <= 0 || retries <= 0 || budgetSeconds <= 0) return false; return checked(intervalSeconds * retries) <= budgetSeconds;
    }
}
