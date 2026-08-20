public static class Submission
{
    public static int AwaitedTotal(int[] completedResults)
    {
        if (completedResults is null)
        {
            throw new System.ArgumentNullException(nameof(completedResults));
        }

        int total = 0;

        foreach (int result in completedResults)
        {
            // Chaque résultat compte exactement une fois ; le cumul vérifié lève au
            // lieu de s'enrouler si le total dépassait la capacité du type.
            total = checked(total + result);
        }

        return total;
    }
}
