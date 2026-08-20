public static class Submission
{
    public static decimal ShippingCost(decimal orderTotal, bool isExpress)
    {
        if (orderTotal < 0m)
        {
            throw new System.ArgumentOutOfRangeException(nameof(orderTotal));
        }

        if (isExpress)
        {
            return 9.90m;
        }

        return orderTotal >= 80m ? 0m : 4.90m;
    }
}
