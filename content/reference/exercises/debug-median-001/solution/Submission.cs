public static class Submission
{
    public static decimal Median(int[] values)
    {
        if (values is null || values.Length == 0) return 0m; int[] copy = (int[])values.Clone(); System.Array.Sort(copy); int middle = copy.Length / 2; return copy.Length % 2 == 1 ? copy[middle] : ((decimal)copy[middle - 1] + copy[middle]) / 2m;
    }
}
