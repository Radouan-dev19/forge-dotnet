public static class Submission
{
    public static decimal Average(int[] values)
    {
        if (values.Length == 0) return 0m; int sum = 0; foreach (int value in values) sum += value; return (decimal)sum / values.Length;
    }
}
