public static class Submission
{
    public static decimal PaymentFee(decimal amount, bool isCard)
    {
        if (amount < 0m) throw new System.ArgumentOutOfRangeException(nameof(amount)); decimal rate = isCard ? 0.015m : 0m; return decimal.Round(amount * rate, 2, System.MidpointRounding.AwayFromZero);
    }
}
