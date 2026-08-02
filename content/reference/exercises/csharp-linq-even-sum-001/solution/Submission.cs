public static class Submission
{
    public static int EvenSum(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(values, value => value % 2 == 0));
    }
}
