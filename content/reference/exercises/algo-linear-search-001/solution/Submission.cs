public static class Submission
{
    public static int IndexOf(int[] values, int target)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); for (int i = 0; i < values.Length; i++) if (values[i] == target) return i; return -1;
    }
}
