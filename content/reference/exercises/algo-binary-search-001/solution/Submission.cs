public static class Submission
{
    public static int BinarySearch(int[] values, int target)
    {
        int left = 0, right = values.Length - 1; while (left <= right) { int middle = left + (right - left) / 2; if (values[middle] == target) return middle; if (values[middle] < target) left = middle + 1; else right = middle - 1; } return -1;
    }
}
