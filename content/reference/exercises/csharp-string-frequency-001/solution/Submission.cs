public static class Submission
{
    public static System.Collections.Generic.Dictionary<string, int> CountWords(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        var counts = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
        var word = new System.Text.StringBuilder();

        void CompleteWord()
        {
            if (word.Length == 0)
            {
                return;
            }

            string key = word.ToString();
            counts.TryGetValue(key, out int current);
            counts[key] = current + 1;
            word.Clear();
        }

        foreach (char character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                word.Append(char.ToLowerInvariant(character));
            }
            else
            {
                CompleteWord();
            }
        }

        CompleteWord();
        return counts;
    }
}
