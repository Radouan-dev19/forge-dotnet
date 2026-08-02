public static class Submission
{
    public static decimal AverageOrZero(int[] values)
    {
        if (values is null || values.Length == 0) return 0m; long sum = 0; foreach (int value in values) sum += value; return (decimal)sum / values.Length;
    }
}
