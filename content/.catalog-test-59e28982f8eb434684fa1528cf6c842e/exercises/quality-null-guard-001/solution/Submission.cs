public static class Submission
{
    public static string NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Trim();
    }
}
