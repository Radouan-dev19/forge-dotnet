public static class Submission
{
    public static int[] ResetState(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return new int[values.Length];
    }
}
