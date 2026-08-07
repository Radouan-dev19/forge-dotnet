public static class Submission
{
    public static bool IsLocalRedirect(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false; return value.StartsWith("/", System.StringComparison.Ordinal) && !value.StartsWith("//", System.StringComparison.Ordinal) && !value.StartsWith("/\\", System.StringComparison.Ordinal);
    }
}
