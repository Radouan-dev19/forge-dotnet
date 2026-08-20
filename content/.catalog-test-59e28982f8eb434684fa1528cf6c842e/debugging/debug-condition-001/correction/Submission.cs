public static class Submission
{
    public static decimal CalculateShipping(decimal total, bool express)
    {
        if (express) return 9.90m;
        return total >= 50m ? 0m : 4.90m;
    }
}
