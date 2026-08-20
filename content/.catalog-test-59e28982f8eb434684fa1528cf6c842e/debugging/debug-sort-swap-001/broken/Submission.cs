public static class Submission
{
    public static int[] SortCopy(int[] values)
    {
        int[] result = (int[])values.Clone(); System.Array.Sort(result); System.Array.Reverse(result); return result;
    }
}
