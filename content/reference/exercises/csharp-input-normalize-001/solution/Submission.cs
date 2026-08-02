public static class Submission
{
    public static string NormalizeInput(string value)
    {
        if (value is null) throw new System.ArgumentNullException(nameof(value)); return value.Trim();
    }
}
