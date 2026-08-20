using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class Submission
{
    public static string SilentCustomers(decimal minimumTotal)
    {
        // Un seuil nul ou négatif viderait la question : toute commande le franchirait.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(minimumTotal, 0m);

        using SqliteConnection connection = CreateDatabase();
        using var context = new ShopContext(CreateOptions(connection));

        // La question est une absence : elle se pose depuis les clients, et la négation de
        // l'existence se traduit en sous-requête serveur — la jointure interne perdrait Margaret.
        IQueryable<string> query = context.Customers
            .Where(customer => !customer.Orders.Any(order => order.Total >= minimumTotal))
            .OrderBy(customer => customer.Name)
            .Select(customer => customer.Name);

        return string.Join(',', query.ToArray());
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "Customers" (
                "CustomerId" INTEGER NOT NULL CONSTRAINT "PK_Customers" PRIMARY KEY,
                "Name" TEXT NOT NULL
            );
            CREATE TABLE "Orders" (
                "OrderId" INTEGER NOT NULL CONSTRAINT "PK_Orders" PRIMARY KEY,
                "CustomerId" INTEGER NOT NULL,
                "Total" TEXT NOT NULL,
                CONSTRAINT "FK_Orders_Customers" FOREIGN KEY ("CustomerId")
                    REFERENCES "Customers" ("CustomerId")
            );
            INSERT INTO "Customers" ("CustomerId", "Name") VALUES
                (1, 'Ada'), (2, 'Grace'), (3, 'Linus'), (4, 'Margaret');
            INSERT INTO "Orders" ("OrderId", "CustomerId", "Total") VALUES
                (1, 1, '120.50'), (2, 1, '40.25'), (3, 2, '75.0'), (4, 3, '15.0');
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static DbContextOptions<ShopContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<ShopContext>().UseSqlite(connection).Options;

    private sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
    }

    private sealed class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Order> Orders { get; set; } = new();
    }

    private sealed class Order
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
    }
}
