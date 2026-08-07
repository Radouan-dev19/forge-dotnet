public static class Submission
{
    public static bool ContainsTerm(string value, string term)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(term)) return false; return value.Contains(term.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
