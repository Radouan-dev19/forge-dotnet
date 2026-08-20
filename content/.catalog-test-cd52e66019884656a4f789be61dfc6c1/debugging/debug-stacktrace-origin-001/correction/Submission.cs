public static class Submission
{
    public static string Origin(string trace)
    {
        foreach (string line in trace.Split('\n')) { string value = line.Trim(); if (value.StartsWith("at Forge.", System.StringComparison.Ordinal)) return value; } return "";
    }
}
