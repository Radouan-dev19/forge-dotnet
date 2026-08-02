public static class Submission
{
    public static bool Balanced(string text)
    {
        int depth = 0; foreach (char character in text) { if (character == 40) depth++; else if (character == 41 && --depth < 0) return false; } return depth == 0;
    }
}
