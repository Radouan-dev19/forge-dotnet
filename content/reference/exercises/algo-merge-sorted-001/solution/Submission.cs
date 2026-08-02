public static class Submission
{
    public static int[] MergeSorted(int[] left, int[] right)
    {
        int[] result = new int[left.Length + right.Length]; int i = 0, j = 0, k = 0; while (i < left.Length || j < right.Length) result[k++] = j >= right.Length || (i < left.Length && left[i] <= right[j]) ? left[i++] : right[j++]; return result;
    }
}
