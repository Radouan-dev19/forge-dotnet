public static class Submission
{
    public static string FormatCustomerName(string value) =>
        string.IsNullOrWhiteSpace(value) ? "(inconnu)" : value.Trim().ToUpperInvariant();
}
