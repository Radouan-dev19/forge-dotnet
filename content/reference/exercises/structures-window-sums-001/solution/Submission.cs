public static class Submission
{
    public static int[] WindowSums(int[] values, int size)
    {
        if (size <= 0 || size > values.Length) return System.Array.Empty<int>(); int[] result = new int[values.Length - size + 1]; int sum = 0; for (int i = 0; i < values.Length; i++) { sum += values[i]; if (i >= size) sum -= values[i - size]; if (i >= size - 1) result[i - size + 1] = sum; } return result;
    }
}
