public static class Submission
{
    public static int UniqueCount(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return new System.Collections.Generic.HashSet<int>(values).Count;
    }
}
