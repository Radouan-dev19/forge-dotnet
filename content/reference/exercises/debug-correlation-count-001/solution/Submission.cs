public static class Submission
{
    public static int CorrelationCount(string log, string correlationId)
    {
        if (string.IsNullOrEmpty(log) || string.IsNullOrEmpty(correlationId)) return 0; int count = 0, index = 0; while ((index = log.IndexOf(correlationId, index, System.StringComparison.Ordinal)) >= 0) { count++; index += correlationId.Length; } return count;
    }
}
