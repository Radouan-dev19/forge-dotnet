using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersQueryableStarter
{
    public static IQueryable<OrderSummary> BuildQuery(MiniErpContext context, decimal minimumTotal) =>
        context.Orders
            .AsNoTracking()
            .OrderBy(item => item.OrderId)
            .Select(item => new OrderSummary(item.OrderId, item.Customer.Name, item.Total));
}
