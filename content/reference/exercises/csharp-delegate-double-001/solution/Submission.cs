public static class Submission
{
    public static int[] DoubleAll(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Array.ConvertAll(values, value => checked(value * 2));
    }
}
