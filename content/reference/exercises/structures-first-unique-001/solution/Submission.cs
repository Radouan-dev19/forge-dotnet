public static class Submission
{
    public static string FirstUnique(string text)
    {
        var counts = new System.Collections.Generic.Dictionary<char, int>(); foreach (char c in text) counts[c] = counts.TryGetValue(c, out int count) ? count + 1 : 1; foreach (char c in text) if (counts[c] == 1) return c.ToString(); return "";
    }
}
