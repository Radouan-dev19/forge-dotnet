public static class Submission
{
    public static int[] AbsoluteAll(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Array.ConvertAll(values, value => System.Math.Abs(value));
    }
}
