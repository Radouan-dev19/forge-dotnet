public static class Submission
{
    public static int RetryDelay(int attempt)
    {
        if (attempt < 0) throw new System.ArgumentOutOfRangeException(nameof(attempt)); int power = System.Math.Min(attempt, 5); return 100 * (1 << power);
    }
}
