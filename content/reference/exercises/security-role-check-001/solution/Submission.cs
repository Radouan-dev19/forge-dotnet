public static class Submission
{
    public static bool HasRole(string roles, string required)
    {
        if (string.IsNullOrWhiteSpace(roles) || string.IsNullOrWhiteSpace(required)) return false; foreach (string role in roles.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)) if (string.Equals(role, required.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true; return false;
    }
}
