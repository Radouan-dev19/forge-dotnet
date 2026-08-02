using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersQueryableSolution
{
    public static IQueryable<OrderSummary> BuildQuery(MiniErpContext context, decimal minimumTotal) =>
        context.Orders
            .AsNoTracking()
            .Where(item => item.Total >= minimumTotal)
            .OrderBy(item => item.OrderId)
            .Select(item => new OrderSummary(item.OrderId, item.Customer.Name, item.Total));
}
