using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersLoadingStarter
{
    public static async Task<IReadOnlyList<CustomerOrderCount>> LoadAsync(
        MiniErpContext context,
        CancellationToken cancellationToken = default)
    {
        List<Customer> customers = await context.Customers.AsNoTracking().OrderBy(item => item.CustomerId).ToListAsync(cancellationToken);
        var results = new List<CustomerOrderCount>(customers.Count);
        foreach (Customer customer in customers)
        {
            int count = await context.Orders.CountAsync(item => item.CustomerId == customer.CustomerId, cancellationToken);
            results.Add(new CustomerOrderCount(customer.CustomerId, customer.Name, count));
        }

        return results;
    }
}
