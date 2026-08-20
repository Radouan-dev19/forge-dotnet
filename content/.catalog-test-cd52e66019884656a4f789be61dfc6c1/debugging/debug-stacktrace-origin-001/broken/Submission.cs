public static class Submission
{
    public static string Origin(string trace)
    {
        string[] lines = trace.Split('\n'); return lines.Length == 0 ? "" : lines[lines.Length - 1].Trim();
    }
}
