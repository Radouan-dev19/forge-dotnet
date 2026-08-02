public static class Submission
{
    public static int[] DistinctSorted(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Distinct(values), value => value));
    }
}
