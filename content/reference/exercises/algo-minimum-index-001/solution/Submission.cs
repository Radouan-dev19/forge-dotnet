public static class Submission
{
    public static int MinimumIndex(int[] values)
    {
        if (values is null || values.Length == 0) return -1; int min = 0; for (int i = 1; i < values.Length; i++) if (values[i] < values[min]) min = i; return min;
    }
}
