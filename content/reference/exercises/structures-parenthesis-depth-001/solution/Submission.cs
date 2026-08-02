public static class Submission
{
    public static int MaximumDepth(string text)
    {
        int depth = 0, maximum = 0; foreach (char c in text) { if (c == 40) { depth++; maximum = System.Math.Max(maximum, depth); } else if (c == 41 && --depth < 0) return -1; } return depth == 0 ? maximum : -1;
    }
}
