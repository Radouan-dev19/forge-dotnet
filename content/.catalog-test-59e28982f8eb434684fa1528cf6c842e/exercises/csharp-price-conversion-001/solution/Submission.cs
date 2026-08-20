public static class Submission
{
    public static int ToCents(decimal amount)
    {
        if (amount < 0m)
        {
            throw new System.ArgumentOutOfRangeException(nameof(amount));
        }

        decimal cents = decimal.Round(amount * 100m, 0, System.MidpointRounding.AwayFromZero);
        return decimal.ToInt32(cents);
    }
}
