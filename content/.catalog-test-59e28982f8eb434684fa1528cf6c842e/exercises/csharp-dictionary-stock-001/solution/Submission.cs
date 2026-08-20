public static class Submission
{
    public static System.Collections.Generic.Dictionary<string, int> MergeStock(
        System.Collections.Generic.Dictionary<string, int> stock,
        System.Collections.Generic.Dictionary<string, int> incoming)
    {
        System.ArgumentNullException.ThrowIfNull(stock);
        System.ArgumentNullException.ThrowIfNull(incoming);
        ValidateQuantities(stock);
        ValidateQuantities(incoming);

        var result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
        MergeInto(result, stock);
        MergeInto(result, incoming);
        return result;
    }

    private static void ValidateQuantities(System.Collections.Generic.Dictionary<string, int> source)
    {
        foreach (var pair in source)
        {
            if (pair.Value < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(source));
            }
        }
    }

    private static void MergeInto(
        System.Collections.Generic.Dictionary<string, int> result,
        System.Collections.Generic.Dictionary<string, int> source)
    {
        foreach (var pair in source)
        {
            string key = pair.Key.ToLowerInvariant();
            result.TryGetValue(key, out int current);
            result[key] = current + pair.Value;
        }
    }
}
