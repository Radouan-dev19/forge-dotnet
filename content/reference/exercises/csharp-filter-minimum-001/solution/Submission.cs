public static class Submission
{
    public static int[] AtLeast(int[] values, int minimum)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(values, value => value >= minimum));
    }
}
