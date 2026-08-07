public static class Submission
{
    public static bool IsIdempotent(string method)
    {
        if (string.IsNullOrWhiteSpace(method)) return false; string value = method.Trim().ToUpperInvariant(); return value is "GET" or "PUT" or "DELETE" or "HEAD" or "OPTIONS";
    }
}
