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
        Order readOnly = context.Orders.AsNoTracking().Single(item => item.OrderId == orderId);

        bool sameTrackedInstance = ReferenceEquals(first, second);
        bool readOnlyEntityIsDetached = context.Entry(readOnly).State == EntityState.Detached;
        return $"{sameTrackedInstance.ToString().ToLowerInvariant()}|{readOnlyEntityIsDetached.ToString().ToLowerInvariant()}|{context.ChangeTracker.Entries().Count()}";
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "Orders" (
                "OrderId" INTEGER NOT NULL CONSTRAINT "PK_Orders" PRIMARY KEY,
                "Total" TEXT NOT NULL
            );
            INSERT INTO "Orders" ("OrderId", "Total") VALUES
                (1, '120.50'),
                (2, '75');
            """;
        command.ExecuteNonQuery();
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
