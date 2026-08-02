public static class Submission
{
    public static int[] SortedCopy(int[] values)
    {
        int[] copy = (int[])values.Clone(); System.Array.Sort(copy); return copy;
    }
}
