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
    public static string ShelfValue(int shelfId)
    {
        using SqliteConnection connection = CreateDatabase();
        var counter = new CommandCounter();
        using var context = new StockContext(CreateOptions(connection, counter));

        // Une seule requête charge les trois niveaux : le rayon, ses articles, leurs mouvements.
        Shelf? shelf = context.Shelves
            .Where(item => item.ShelfId == shelfId)
            .Include(item => item.Items)
            .ThenInclude(item => item.Movements)
            .SingleOrDefault();
        if (shelf is null)
        {
            throw new ArgumentOutOfRangeException(nameof(shelfId));
        }

        // L'agrégation décimale se fait en mémoire : SQLite stocke ces montants en texte.
        decimal total = shelf.Items
            .Sum(item => item.UnitPrice * item.Movements.Sum(movement => movement.Quantity));
        return $"{Money(total)}|{counter.Count}";
    }

    public static string ShelfRank(int take)
    {
        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take));
        }

        using SqliteConnection connection = CreateDatabase();
        var counter = new CommandCounter();
        using var context = new StockContext(CreateOptions(connection, counter));

        // La projection compte côté base : le nombre de requêtes ne dépend plus du nombre de rayons.
        List<string> names = context.Shelves
            .Select(shelf => new
            {
                shelf.Name,
                Count = shelf.Items.SelectMany(item => item.Movements).Count(),
            })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Name)
            .Take(take)
            .Select(entry => entry.Name)
            .ToList();
        return $"{string.Join(",", names)}|{counter.Count}";
    }

    public static string ItemPage(int page, int size)
    {
        if (page <= 0 || size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        using SqliteConnection connection = CreateDatabase();
        var counter = new CommandCounter();
        using var context = new StockContext(CreateOptions(connection, counter));

        // Tri, saut et taille s'exécutent côté base : une page coûte une requête, quelle que
        // soit sa taille, et la base ne rapatrie que la tranche demandée.
        List<int> identifiers = context.Items
            .OrderBy(item => item.ItemId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(item => item.ItemId)
            .ToList();

        return identifiers.Count == 0
            ? $"aucun|{counter.Count}"
            : $"{string.Join(",", identifiers)}|{counter.Count}";
    }

    // ======================= FOURNI — ne pas modifier =======================

    public sealed class Shelf
    {
        public int ShelfId { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Item> Items { get; set; } = [];
    }

    public sealed class Item
    {
        public int ItemId { get; set; }

        public int ShelfId { get; set; }

        public decimal UnitPrice { get; set; }

        public Shelf? Shelf { get; set; }

        public List<Movement> Movements { get; set; } = [];
    }

    public sealed class Movement
    {
        public int MovementId { get; set; }

        public int ItemId { get; set; }

        public int Quantity { get; set; }

        public Item? Item { get; set; }
    }

    public sealed class StockContext(DbContextOptions<StockContext> options) : DbContext(options)
    {
        public DbSet<Shelf> Shelves => Set<Shelf>();

        public DbSet<Item> Items => Set<Item>();

        public DbSet<Movement> Movements => Set<Movement>();
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
            "CREATE TABLE Shelves (ShelfId INTEGER PRIMARY KEY, Name TEXT NOT NULL);"
            + "CREATE TABLE Items (ItemId INTEGER PRIMARY KEY, ShelfId INTEGER NOT NULL,"
            + " UnitPrice TEXT NOT NULL);"
            + "CREATE TABLE Movements (MovementId INTEGER PRIMARY KEY, ItemId INTEGER NOT NULL,"
            + " Quantity INTEGER NOT NULL);"
            + "INSERT INTO Shelves VALUES (1, 'Papeterie'), (2, 'Informatique'), (3, 'Mobilier');"
            + "INSERT INTO Items VALUES (1, 1, '2.50'), (2, 1, '1.20'), (3, 1, '4.00'),"
            + " (4, 2, '120.00'), (5, 2, '35.50'), (6, 3, '89.90');"
            + "INSERT INTO Movements VALUES (1, 1, 4), (2, 1, 2), (3, 2, 5), (4, 3, 1), (5, 3, 2),"
            + " (6, 4, 2), (7, 5, 3), (8, 5, 1), (9, 6, 1);";
        command.ExecuteNonQuery();
        return connection;
    }

    public static DbContextOptions<StockContext> CreateOptions(
        SqliteConnection connection, CommandCounter? counter = null)
    {
        DbContextOptionsBuilder<StockContext> builder =
            new DbContextOptionsBuilder<StockContext>().UseSqlite(connection);
        return counter is null ? builder.Options : builder.AddInterceptors(counter).Options;
    }

    public static string Money(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero).ToString("F2", CultureInfo.InvariantCulture);
}
