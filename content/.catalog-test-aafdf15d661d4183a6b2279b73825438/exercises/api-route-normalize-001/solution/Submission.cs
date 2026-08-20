public static class Submission
{
    public static string NormalizeRoute(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ""; return value.Trim().Trim('/').ToLowerInvariant();
    }
}
