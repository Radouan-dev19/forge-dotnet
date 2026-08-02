public static class Submission
{
    public static int PositiveSum(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); int sum = 0; foreach (int value in values) if (value > 0) sum = checked(sum + value); return sum;
    }
}
