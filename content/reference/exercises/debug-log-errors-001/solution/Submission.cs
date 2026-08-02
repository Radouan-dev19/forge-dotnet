public static class Submission
{
    public static int ErrorCount(string log)
    {
        if (string.IsNullOrEmpty(log)) return 0; int count = 0, index = 0; while ((index = log.IndexOf("ERROR", index, System.StringComparison.Ordinal)) >= 0) { count++; index += 5; } return count;
    }
}
