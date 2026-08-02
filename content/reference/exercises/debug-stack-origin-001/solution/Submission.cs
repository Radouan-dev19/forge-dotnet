public static class Submission
{
    public static string FirstFrame(string trace)
    {
        if (string.IsNullOrWhiteSpace(trace)) return ""; foreach (string line in trace.Split('\n')) { string trimmed = line.Trim(); if (trimmed.StartsWith("at Forge.", System.StringComparison.Ordinal)) return trimmed; } return "";
    }
}
