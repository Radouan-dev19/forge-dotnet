public static class Submission
{
    public static string ExtensionOf(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return ""; return System.IO.Path.GetExtension(path).ToLowerInvariant();
    }
}
