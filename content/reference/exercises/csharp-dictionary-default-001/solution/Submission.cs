public static class Submission
{
    public static int StockFor(System.Collections.Generic.Dictionary<string, int> stock, string key)
    {
        if (stock is null) throw new System.ArgumentNullException(nameof(stock)); if (string.IsNullOrWhiteSpace(key)) return 0; return stock.TryGetValue(key, out int value) ? value : 0;
    }
}
