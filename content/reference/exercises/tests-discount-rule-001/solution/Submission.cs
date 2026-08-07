public static class Submission
{
    public static decimal DiscountRate(decimal total)
    {
        if (total < 0m) throw new System.ArgumentOutOfRangeException(nameof(total)); if (total >= 200m) return 0.15m; if (total >= 100m) return 0.05m; return 0m;
    }
}
