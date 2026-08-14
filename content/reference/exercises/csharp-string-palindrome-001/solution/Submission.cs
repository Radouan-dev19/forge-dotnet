public static class Submission
{
    public static bool IsPalindrome(string value)
    {
        if (value is null)
        {
            throw new System.ArgumentNullException(nameof(value));
        }

        // Normalisation annoncée par l'énoncé : espaces retirés, casse aplanie.
        string text = value.Replace(" ", "", System.StringComparison.Ordinal).ToLowerInvariant();

        // Deux index qui convergent : chaque paire symétrique est comparée une fois.
        for (int left = 0, right = text.Length - 1; left < right; left++, right--)
        {
            if (text[left] != text[right])
            {
                return false;
            }
        }

        // Index croisés sans différence : la chaîne se lit dans les deux sens.
        return true;
    }
}
