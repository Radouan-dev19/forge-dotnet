public static class Submission
{
    public static int WindowTotal(int[] values, int size)
    {
        if (size <= 0) return 0; int sum = 0; for (int i = System.Math.Max(0, values.Length - size); i < values.Length; i++) sum += values[i]; return sum;
    }
}
