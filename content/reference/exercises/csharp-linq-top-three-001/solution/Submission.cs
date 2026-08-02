public static class Submission
{
    public static int[] TopThree(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(values, value => value), 3));
    }
}
