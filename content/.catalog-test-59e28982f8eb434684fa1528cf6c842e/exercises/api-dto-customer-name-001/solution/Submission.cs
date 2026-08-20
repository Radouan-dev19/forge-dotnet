public static class Submission
{
    public static string CustomerLabel(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "(invalide)" : name.Trim();
    }
}
