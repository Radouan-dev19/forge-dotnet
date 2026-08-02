public static class Submission
{
    public static int RunCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0; int runs = 1; for (int i = 1; i < text.Length; i++) if (text[i] != text[i - 1]) runs++; return runs;
    }
}
