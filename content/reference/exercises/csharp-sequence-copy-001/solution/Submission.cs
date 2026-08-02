public static class Submission
{
    public static System.Collections.Generic.List<int> CopyValues(System.Collections.Generic.List<int> values)
    {
        if (values is null) throw new System.ArgumentNullException(nameof(values)); return new System.Collections.Generic.List<int>(values);
    }
}
