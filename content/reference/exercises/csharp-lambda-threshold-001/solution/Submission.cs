public static class Submission
{
    public static int CountAtLeast(int[] values, int minimum)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.Count(values, value => value >= minimum);
    }
}
