using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class Submission
{
    public static Dictionary<string, int> StatusCounts(int minimumCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(minimumCount, 0);

        using SqliteConnection connection = CreateDatabase();
        using var context = new ShopContext(CreateOptions(connection));

        // Écrivez ici la requête sur context.Orders : regroupement, filtre de groupe, projection.
        throw new NotImplementedException();
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "Orders" (
                "OrderId" INTEGER NOT NULL CONSTRAINT "PK_Orders" PRIMARY KEY,
                "Status" TEXT NOT NULL
            );
            INSERT INTO "Orders" ("OrderId", "Status") VALUES
                (1, 'shipped'), (2, 'pending'), (3, 'shipped'),
                (4, 'canceled'), (5, 'pending'), (6, 'shipped');
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static DbContextOptions<ShopContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<ShopContext>().UseSqlite(connection).Options;

    private sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
    }

    private sealed class Order
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
