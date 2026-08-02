public static class Submission
{
    public static int CountEven(int[] values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); int count = 0; foreach (int value in values) if (value % 2 == 0) count++; return count;
    }
}
