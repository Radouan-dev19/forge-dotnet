using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class Submission
{
    public static string OrderSummary(int orderId)
    {
        using SqliteConnection connection = CreateDatabase();
        using var context = new OrdersContext(CreateOptions(connection));

        // Une seule requête rapatrie la commande, son client et ses lignes : le résumé ne fait
        // ensuite qu'agréger en mémoire, parce que SQLite ne sait pas additionner un décimal
        // stocké en texte.
        Order? order = context.Orders
            .Include(item => item.Customer)
            .Include(item => item.Lines)
            .FirstOrDefault(item => item.OrderId == orderId);
        if (order is null)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        decimal total = order.Lines.Sum(line => line.UnitPrice * line.Quantity);
        return $"{order.Customer!.Name}|{order.Lines.Count}|{Money(total)}";
    }

    public static int LoadRoundTrips(int customerId)
    {
        var counter = new CommandCounter();
        using SqliteConnection connection = CreateDatabase();
        using var context = new OrdersContext(CreateOptions(connection, counter));

        // Charger les trois niveaux d'un seul tenant : sans les inclusions, chaque commande puis
        // chaque ligne partiraient en requêtes séparées — le défaut dit « N plus un ».
        Customer? customer = context.Customers
            .Include(item => item.Orders)
            .ThenInclude(order => order.Lines)
            .FirstOrDefault(item => item.CustomerId == customerId);
        if (customer is null)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        return counter.Count;
    }

    public static string ApplyDiscount(int orderId, decimal rate)
    {
        if (rate < 0m || rate > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        using SqliteConnection connection = CreateDatabase();

        using (var context = new OrdersContext(CreateOptions(connection)))
        {
            Order? order = context.Orders
                .Include(item => item.Lines)
                .FirstOrDefault(item => item.OrderId == orderId);
            if (order is null)
            {
                throw new ArgumentOutOfRangeException(nameof(orderId));
            }

            foreach (OrderLine line in order.Lines)
            {
                line.UnitPrice = decimal.Round(line.UnitPrice * (1m - rate), 2, MidpointRounding.AwayFromZero);
            }

            context.SaveChanges();
        }

        // Contexte neuf : relire prouve que l'écriture est allée jusqu'à la base, et non
        // seulement jusqu'au suivi des entités du premier contexte.
        using var verification = new OrdersContext(CreateOptions(connection));
        List<OrderLine> lines = verification.OrderLines.Where(line => line.OrderId == orderId).ToList();
        return Money(lines.Sum(line => line.UnitPrice * line.Quantity));
    }

    // ======================= FOURNI — ne pas modifier =======================

    public sealed class Customer
    {
        public int CustomerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Order> Orders { get; set; } = [];
    }

    public sealed class Order
    {
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public List<OrderLine> Lines { get; set; } = [];
    }

    public sealed class OrderLine
    {
        public int OrderLineId { get; set; }

        public int OrderId { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }

    public sealed class OrdersContext(DbContextOptions<OrdersContext> options) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    }

    /// <summary>Compte les commandes SQL réellement envoyées à la base.</summary>
    public sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }
    }

    /// <summary>Ouvre une base en mémoire et y installe le jeu de données de référence.</summary>
    public static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using DbCommand command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE Customers (CustomerId INTEGER PRIMARY KEY, Name TEXT NOT NULL);"
            + "CREATE TABLE Orders (OrderId INTEGER PRIMARY KEY, CustomerId INTEGER NOT NULL);"
            + "CREATE TABLE OrderLines (OrderLineId INTEGER PRIMARY KEY, OrderId INTEGER NOT NULL,"
            + " UnitPrice TEXT NOT NULL, Quantity INTEGER NOT NULL);"
            + "INSERT INTO Customers VALUES (1, 'Alice'), (2, 'Bruno');"
            + "INSERT INTO Orders VALUES (10, 1), (11, 1), (20, 2), (21, 2);"
            + "INSERT INTO OrderLines VALUES (1, 10, '10.00', 2), (2, 10, '5.50', 4),"
            + " (3, 11, '3.00', 1), (4, 20, '7.25', 4), (5, 20, '1.00', 3), (6, 20, '2.00', 1),"
            + " (7, 21, '12.34', 2);";
        command.ExecuteNonQuery();
        return connection;
    }

    public static DbContextOptions<OrdersContext> CreateOptions(
        SqliteConnection connection, CommandCounter? counter = null)
    {
        DbContextOptionsBuilder<OrdersContext> builder =
            new DbContextOptionsBuilder<OrdersContext>().UseSqlite(connection);
        return counter is null ? builder.Options : builder.AddInterceptors(counter).Options;
    }

    public static string Money(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero).ToString("F2", CultureInfo.InvariantCulture);
}
