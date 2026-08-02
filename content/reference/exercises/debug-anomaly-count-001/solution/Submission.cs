public static class Submission
{
    public static int AnomalyCount(int[] values, int threshold)
    {
        int count = 0; foreach (int value in values) if (System.Math.Abs((long)value) > threshold) count++; return count;
    }
}
