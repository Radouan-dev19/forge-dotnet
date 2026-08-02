public static class Submission
{
    public static decimal LineTotal(decimal unitPrice, int quantity)
    {
        if (unitPrice < 0m || quantity < 0) throw new System.ArgumentOutOfRangeException(); return decimal.Round(unitPrice * quantity, 2, System.MidpointRounding.AwayFromZero);
    }
}
