public static class Submission
{
    public static string StatusLabel(int status)
    {
        return status switch { 0 => "draft", 1 => "paid", 2 => "shipped", _ => "unknown" };
    }
}
