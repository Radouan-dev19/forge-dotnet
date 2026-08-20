public static class Submission
{
    public static decimal CalculateShipping(decimal total, bool express)
    {
        if (total >= 50m) return 0m;
        if (express) return 9.90m;
        return 4.90m;
    }
}
