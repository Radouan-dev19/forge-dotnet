public static class Submission
{
    public static decimal ApplyDiscount(decimal total, decimal rate)
    {
        if (total < 0m || rate < 0m || rate > 1m) throw new System.ArgumentOutOfRangeException(); return decimal.Round(total * (1m - rate), 2, System.MidpointRounding.AwayFromZero);
    }
}
