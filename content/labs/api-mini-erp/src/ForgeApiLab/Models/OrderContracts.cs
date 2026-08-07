using System.ComponentModel.DataAnnotations;

namespace ForgeApiLab.Models;

public sealed record CreateOrderRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Customer,
    [Range(1, 100)] int Quantity);

public sealed record OrderResponse(int Id, string Customer, int Quantity, DateTimeOffset CreatedAtUtc);
