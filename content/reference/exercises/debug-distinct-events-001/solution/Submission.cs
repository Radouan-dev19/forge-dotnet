public static class Submission
{
    public static int DistinctEventCount(string events)
    {
        if (string.IsNullOrWhiteSpace(events)) return 0; return new System.Collections.Generic.HashSet<string>(events.Split(",", System.StringSplitOptions.RemoveEmptyEntries), System.StringComparer.Ordinal).Count;
    }
}
