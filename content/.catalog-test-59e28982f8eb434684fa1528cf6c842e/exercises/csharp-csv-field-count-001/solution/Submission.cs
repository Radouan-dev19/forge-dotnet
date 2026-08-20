public static class Submission
{
    public static int FieldCount(string line)
    {
        if (string.IsNullOrEmpty(line)) return 0; return line.Split(",", System.StringSplitOptions.None).Length;
    }
}
