using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class Submission
{
    public static Dictionary<string, int> StatusCounts(int minimumCount)
    {
        // À zéro, tout groupe passerait : le filtre ne filtrerait rien.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(minimumCount, 0);

        using SqliteConnection connection = CreateDatabase();
        using var context = new ShopContext(CreateOptions(connection));

        // Le plancher est une propriété du groupe : posé après le regroupement, il se traduit en
        // clause de filtrage de groupes côté serveur — posé avant, il filtrerait des lignes.
        var counts = context.Orders
            .GroupBy(order => order.Status)
            .Where(group => group.Count() >= minimumCount)
            .Select(group => new { group.Key, Count = group.Count() });

        return counts.ToDictionary(item => item.Key, item => item.Count);
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
