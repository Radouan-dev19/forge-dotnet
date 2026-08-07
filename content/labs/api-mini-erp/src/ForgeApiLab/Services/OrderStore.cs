using System.Collections.Concurrent;
using ForgeApiLab.Models;

namespace ForgeApiLab.Services;

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, OrderResponse> _orders = new();
    private int _nextId;

    public OrderStore()
    {
        var seed = new OrderResponse(1, "Ada", 2, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
        _orders[seed.Id] = seed;
        _nextId = seed.Id;
    }

    public ValueTask<OrderResponse?> FindAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_orders.GetValueOrDefault(id));
    }

    public ValueTask<IReadOnlyList<OrderResponse>> ListAsync(int page, int pageSize, string sort, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<OrderResponse> ordered = sort == "customer"
            ? _orders.Values.OrderBy(order => order.Customer, StringComparer.Ordinal).ThenBy(order => order.Id)
            : _orders.Values.OrderBy(order => order.Id);
        return ValueTask.FromResult<IReadOnlyList<OrderResponse>>(
            ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray());
    }

    public ValueTask<OrderResponse> AddAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int id = Interlocked.Increment(ref _nextId);
        var created = new OrderResponse(id, request.Customer.Trim(), request.Quantity, DateTimeOffset.UtcNow);
        _orders[id] = created;
        return ValueTask.FromResult(created);
    }
}
