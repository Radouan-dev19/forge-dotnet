public static class Submission
{
    public static string DisplayName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "(inconnu)" : name.Trim().ToUpperInvariant();
    }
}
