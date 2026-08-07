public static class Submission
{
    public static bool IsIsolatedDatabase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false; return name.StartsWith("forge-test-", System.StringComparison.Ordinal) && name.Length >= 20;
    }
}
