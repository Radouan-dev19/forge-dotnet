using ForgeTestingLab;

namespace ForgeTestingLab.Tests;

public sealed class OrderPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 5);

    [Theory]
    [InlineData(99.99, 99.99)]
    [InlineData(100, 95)]
    [InlineData(200, 170)]
    public void NetTotalUsesDiscountBoundaries(double total, double expected) =>
        Assert.Equal((decimal)expected, OrderPolicy.NetTotal((decimal)total, 1, Today, Today));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void NetTotalRejectsQuantityOutsideInclusiveRange(int quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderPolicy.NetTotal(10m, quantity, Today, Today));

    [Fact]
    public void NetTotalUsesInjectedDateAtExpiryBoundary()
    {
        Assert.Equal(10m, OrderPolicy.NetTotal(10m, 1, Today, Today));
        Assert.Throws<InvalidOperationException>(() => OrderPolicy.NetTotal(10m, 1, Today.AddDays(-1), Today));
    }
}
