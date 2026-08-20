public static class Submission
{
    public static int ParseOrZero(string text)
    {
        return int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value) ? value : 0;
    }
}
