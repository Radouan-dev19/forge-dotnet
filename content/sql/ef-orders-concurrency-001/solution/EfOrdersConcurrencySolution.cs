using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersConcurrencySolution
{
    public static async Task<bool> UpdateConcurrentlyAsync(
        MiniErpContext firstContext,
        MiniErpContext secondContext,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        Order first = await firstContext.Orders.SingleAsync(item => item.OrderId == orderId, cancellationToken);
        Order second = await secondContext.Orders.SingleAsync(item => item.OrderId == orderId, cancellationToken);
        first.Total += 1m;
        await firstContext.SaveChangesAsync(cancellationToken);
        second.Total += 2m;
        try
        {
            await secondContext.SaveChangesAsync(cancellationToken);
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
    }
}
