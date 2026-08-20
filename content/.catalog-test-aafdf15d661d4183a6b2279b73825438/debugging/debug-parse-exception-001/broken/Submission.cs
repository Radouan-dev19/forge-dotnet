public static class Submission
{
    public static int ParseOrZero(string text)
    {
        return int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }
}
