public static class Submission
{
    public static System.Collections.Generic.Dictionary<string, int> WordHistogram(string text)
    {
        var result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase); if (string.IsNullOrWhiteSpace(text)) return result; foreach (string word in text.Split(" ", System.StringSplitOptions.RemoveEmptyEntries)) result[word] = result.TryGetValue(word, out int count) ? count + 1 : 1; return result;
    }
}
