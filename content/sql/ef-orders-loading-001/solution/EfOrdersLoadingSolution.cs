using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersLoadingSolution
{
    public static async Task<IReadOnlyList<CustomerOrderCount>> LoadAsync(
        MiniErpContext context,
        CancellationToken cancellationToken = default)
    {
        List<Customer> customers = await context.Customers
            .AsNoTracking()
            .Include(item => item.Orders)
            .OrderBy(item => item.CustomerId)
            .ToListAsync(cancellationToken);
        return customers
            .Select(item => new CustomerOrderCount(item.CustomerId, item.Name, item.Orders.Count))
            .ToArray();
    }
}
