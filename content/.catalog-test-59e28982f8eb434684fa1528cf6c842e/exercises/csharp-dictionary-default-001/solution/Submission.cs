public static class Submission
{
    public static int StockFor(System.Collections.Generic.Dictionary<string, int> stock, string key)
    {
        // Pas de dictionnaire du tout : faute d'appel, distincte d'un stock à zéro.
        if (stock is null)
        {
            throw new System.ArgumentNullException(nameof(stock));
        }

        // Une clé absente ou blanche ne désigne aucune référence : stock nul par convention.
        if (string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        // TryGetValue interroge sans lever ni écrire : la lecture reste une lecture.
        return stock.TryGetValue(key, out int value) ? value : 0;
    }
}
