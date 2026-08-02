public static class Submission
{
    public static int[] SanitizeCopy(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); int[] copy = new int[values.Length]; for (int i = 0; i < values.Length; i++) copy[i] = System.Math.Max(0, values[i]); return copy;
    }
}
