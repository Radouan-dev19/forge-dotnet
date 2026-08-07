public static class Submission
{
    public static bool HasBearerToken(string header)
    {
        if (string.IsNullOrWhiteSpace(header)) return false; const string prefix = "Bearer "; return header.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) && header.Length > prefix.Length && !string.IsNullOrWhiteSpace(header.Substring(prefix.Length));
    }
}
