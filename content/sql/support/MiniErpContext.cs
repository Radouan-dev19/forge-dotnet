using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public sealed class MiniErpContext(DbContextOptions<MiniErpContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.ToTable("Customers");
            customer.HasKey(item => item.CustomerId);
            customer.Property(item => item.Name).HasMaxLength(80).IsRequired();
            customer.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(item => item.OrderId);
            order.Property(item => item.Total).HasPrecision(10, 2);
            order.Property(item => item.Status).HasMaxLength(20).IsRequired();
            order.Property(item => item.RowVersion).IsRowVersion();
            order.HasIndex(item => new { item.CustomerId, item.CreatedAtUtc })
                .HasDatabaseName("IX_Orders_CustomerId_CreatedAt");
            order.HasOne(item => item.Customer)
                .WithMany(customer => customer.Orders)
                .HasForeignKey(item => item.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class Customer
{
    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public ICollection<Order> Orders { get; } = new List<Order>();
}

public sealed class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Customer Customer { get; set; } = null!;
}

public sealed record OrderSummary(int OrderId, string CustomerName, decimal Total);

public sealed record CustomerOrderCount(int CustomerId, string CustomerName, int OrderCount);
