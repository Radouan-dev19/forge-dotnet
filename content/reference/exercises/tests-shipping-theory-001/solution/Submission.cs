public static class Submission
{
    public static decimal ShippingCost(decimal total, bool express)
    {
        if (total < 0m) throw new System.ArgumentOutOfRangeException(nameof(total)); if (express) return 9.90m; return total >= 50m ? 0m : 4.90m;
    }
}
