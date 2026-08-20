public static class Submission
{
    public static int WindowTotal(int[] values, int size)
    {
        int sum = 0; for (int i = 0; i < values.Length; i++) sum += values[i]; return sum;
    }
}
