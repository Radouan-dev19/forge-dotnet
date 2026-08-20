public static class Submission
{
    public static string Fallback(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Trim();
    }
}
