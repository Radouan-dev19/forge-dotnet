public static class Submission
{
    public static string NormalizeSort(string value)
    {
        string sort = value?.Trim().ToLowerInvariant() ?? ""; return sort is "date" or "total" or "status" ? sort : "id";
    }
}
