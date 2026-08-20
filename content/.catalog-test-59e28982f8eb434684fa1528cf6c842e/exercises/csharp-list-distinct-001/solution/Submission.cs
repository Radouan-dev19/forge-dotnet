public static class Submission
{
    public static System.Collections.Generic.List<int> DistinctInOrder(
        System.Collections.Generic.List<int> values)
    {
        System.ArgumentNullException.ThrowIfNull(values);
        var seen = new System.Collections.Generic.HashSet<int>();
        var result = new System.Collections.Generic.List<int>();
        foreach (int value in values)
        {
            if (seen.Add(value))
            {
                result.Add(value);
            }
        }

        return result;
    }
}
