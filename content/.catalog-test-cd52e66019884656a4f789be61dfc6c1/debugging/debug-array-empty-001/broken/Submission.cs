public static class Submission
{
    public static decimal Average(int[] values)
    {
        int sum = 0; foreach (int value in values) sum += value; return (decimal)sum / values.Length;
    }
}
