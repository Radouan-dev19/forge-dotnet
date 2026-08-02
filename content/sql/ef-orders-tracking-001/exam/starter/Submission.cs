using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class Submission
{
    public static string Run(int orderId)
    {
        using SqliteConnection connection = CreateDatabase();
        using var context = new ExamContext(CreateOptions(connection));
        Order first = context.Orders.Single(item => item.OrderId == orderId);
        Order second = context.Orders.Single(item => item.OrderId == orderId);

        // Corrigez cette lecture : elle est destinée à un affichage sans modification.
        Order readOnly = context.Orders.Single(item => item.OrderId == orderId);

        bool sameTrackedInstance = ReferenceEquals(first, second);
        bool readOnlyEntityIsDetached = context.Entry(readOnly).State == EntityState.Detached;
        return $"{sameTrackedInstance.ToString().ToLowerInvariant()}|{readOnlyEntityIsDetached.ToString().ToLowerInvariant()}|{context.ChangeTracker.Entries().Count()}";
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new ExamContext(CreateOptions(connection));
        context.Database.EnsureCreated();
        context.Orders.AddRange(
            new Order { OrderId = 1, Total = 120.50m },
            new Order { OrderId = 2, Total = 75m });
        context.SaveChanges();
        return connection;
    }

    private static DbContextOptions<ExamContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<ExamContext>().UseSqlite(connection).Options;

    private sealed class ExamContext(DbContextOptions<ExamContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
    }

    private sealed class Order
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
    }
}
