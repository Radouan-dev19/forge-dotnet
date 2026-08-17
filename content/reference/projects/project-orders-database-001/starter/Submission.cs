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
    // ---------------------------------------------------------------------------------------
    // À VOUS : les trois méthodes ci-dessous. Le modèle, le jeu de données et le compteur de
    // commandes sont fournis plus bas — les modifier ferait échouer vos propres cas.
    // ---------------------------------------------------------------------------------------

    /// <summary>Rend « client|nombreDeLignes|total » pour la commande demandée.</summary>
    public static string OrderSummary(int orderId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Rend le nombre de commandes SQL émises pour charger un client, ses commandes et leurs lignes.
    /// </summary>
    public static int LoadRoundTrips(int customerId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Applique une remise à chaque ligne d'une commande, enregistre, relit dans un contexte neuf
    /// et rend le total obtenu.
    /// </summary>
    public static string ApplyDiscount(int orderId, decimal rate)
    {
        throw new NotImplementedException();
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
