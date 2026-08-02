public static class Submission
{
    public static bool IsPalindrome(string value)
    {
        if (value is null) throw new System.ArgumentNullException(nameof(value)); string text = value.Replace(" ", "", System.StringComparison.Ordinal).ToLowerInvariant(); for (int left = 0, right = text.Length - 1; left < right; left++, right--) if (text[left] != text[right]) return false; return true;
    }
}
