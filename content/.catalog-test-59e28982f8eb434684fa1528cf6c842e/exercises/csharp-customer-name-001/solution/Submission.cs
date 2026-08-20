public static class Submission
{
    public static string NormalizeCustomer(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "(inconnu)"; return name.Trim().ToUpperInvariant();
    }
}
