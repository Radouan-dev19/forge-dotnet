public static class Submission
{
    public static int[] SortedCopy(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); int[] copy = (int[])values.Clone(); System.Array.Sort(copy); return copy;
    }
}
