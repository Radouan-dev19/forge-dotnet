public static class Submission
{
    public static int[] InsertionSort(int[] values)
    {
        int[] result = (int[])values.Clone(); for (int i = 1; i < result.Length; i++) { int current = result[i], j = i - 1; while (j >= 0 && result[j] > current) { result[j + 1] = result[j]; j--; } result[j + 1] = current; } return result;
    }
}
