public static class Submission
{
    public static string LastCharacter(string text)
    {
        return string.IsNullOrEmpty(text) ? "" : text[text.Length - 1].ToString();
    }
}
