public static class Submission
{
    public static int[] SelectionSort(int[] values)
    {
        int[] result = (int[])values.Clone(); for (int start = 0; start < result.Length; start++) { int min = start; for (int i = start + 1; i < result.Length; i++) if (result[i] < result[min]) min = i; (result[start], result[min]) = (result[min], result[start]); } return result;
    }
}
