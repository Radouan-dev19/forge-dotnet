public static class Submission
{
    public static int PositiveCount(int[] values)
    {
        return System.Linq.Enumerable.Count(values, value => value >= 0);
    }
}
