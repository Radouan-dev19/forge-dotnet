public static class Submission
{
    public static System.Collections.Generic.Dictionary<string, int> Frequencies(int[] values)
    {
        var result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal); foreach (int value in values) { string key = value.ToString(System.Globalization.CultureInfo.InvariantCulture); result[key] = result.TryGetValue(key, out int count) ? count + 1 : 1; } return result;
    }
}
