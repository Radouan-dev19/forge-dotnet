using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersTrackingStarter
{
    public static async Task<(bool SameTrackedInstance, bool ReadOnlyEntityIsDetached, int TrackedEntries)> ObserveAsync(
        MiniErpContext context,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        Order first = await context.Orders.SingleAsync(item => item.OrderId == orderId, cancellationToken);
        Order second = await context.Orders.SingleAsync(item => item.OrderId == orderId, cancellationToken);
        return (ReferenceEquals(first, second), context.Entry(second).State == EntityState.Detached, context.ChangeTracker.Entries().Count());
    }
}
