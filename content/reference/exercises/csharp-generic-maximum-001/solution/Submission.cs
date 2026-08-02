public static class Submission
{
    public static int MaximumOrZero(int[] values)
    {
        if (values is null || values.Length == 0) return 0; int max = values[0]; foreach (int value in values) if (value > max) max = value; return max;
    }
}
