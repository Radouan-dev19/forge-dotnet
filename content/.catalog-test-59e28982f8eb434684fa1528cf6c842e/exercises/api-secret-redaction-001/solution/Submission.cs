public static class Submission
{
    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value)) return ""; return new string('*', System.Math.Max(4, value.Length));
    }
}
