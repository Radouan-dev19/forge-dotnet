using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class Submission
{
    public static int[] NextPage(int afterId, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(afterId);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 50);

        using SqliteConnection connection = CreateDatabase();
        using var context = new ShopContext(CreateOptions(connection));

        // Écrivez ici la requête sur context.Orders : filtre par curseur, ordre, limitation.
        throw new NotImplementedException();
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "Orders" (
                "OrderId" INTEGER NOT NULL CONSTRAINT "PK_Orders" PRIMARY KEY
            );
            INSERT INTO "Orders" ("OrderId") VALUES
                (8), (3), (21), (14), (35), (11), (27);
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
    }
}
