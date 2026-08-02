public static class Submission
{
    public static int[] BubbleSort(int[] values)
    {
        int[] result = (int[])values.Clone(); for (int end = result.Length - 1; end > 0; end--) for (int i = 0; i < end; i++) if (result[i] > result[i + 1]) (result[i], result[i + 1]) = (result[i + 1], result[i]); return result;
    }
}
