public static class Submission
{
    public static decimal NetTotal(decimal total, bool isVip)
    {
        if (total < 0m) throw new System.ArgumentOutOfRangeException(nameof(total)); decimal rate = isVip ? 0.10m : 0m; return decimal.Round(total * (1m - rate), 2, System.MidpointRounding.AwayFromZero);
    }
}
