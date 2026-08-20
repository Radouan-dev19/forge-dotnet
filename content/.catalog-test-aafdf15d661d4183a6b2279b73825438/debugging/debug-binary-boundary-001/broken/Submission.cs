public static class Submission
{
    public static bool Contains(int[] values, int target)
    {
        int left = 0, right = values.Length - 1; while (left < right) { int middle = (left + right) / 2; if (values[middle] == target) return true; if (values[middle] < target) left = middle + 1; else right = middle - 1; } return false;
    }
}
