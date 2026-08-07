public static class Submission
{
    public static bool HasConflictMarkers(string text)
    {
        if (string.IsNullOrEmpty(text)) return false; return text.Contains("<<<<<<<", System.StringComparison.Ordinal) || text.Contains("=======", System.StringComparison.Ordinal) || text.Contains(">>>>>>>", System.StringComparison.Ordinal);
    }
}
