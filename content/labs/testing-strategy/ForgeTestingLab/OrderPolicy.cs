namespace ForgeTestingLab;

public static class OrderPolicy
{
    public static decimal NetTotal(decimal total, int quantity, DateOnly expiresOn, DateOnly today)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, 100);
        if (expiresOn < today) throw new InvalidOperationException("Offre expirée.");
        decimal rate = total >= 200m ? 0.15m : total >= 100m ? 0.05m : 0m;
        return decimal.Round(total * (1m - rate), 2, MidpointRounding.AwayFromZero);
    }
}
