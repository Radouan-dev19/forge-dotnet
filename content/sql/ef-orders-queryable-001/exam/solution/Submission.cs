using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class Submission
{
    public static string Run(decimal minimumTotal)
    {
        using SqliteConnection connection = CreateDatabase();
        using var context = new ExamContext(CreateOptions(connection));
        IQueryable<int> query = context.Orders
            .AsNoTracking()
            .Where(item => item.Total >= minimumTotal)
            .OrderBy(item => item.OrderId)
            .Select(item => item.OrderId);

        string sql = query.ToQueryString();
        string ids = string.Join(',', query.ToArray());
        bool filteredBySql = sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase);
        return $"{ids}|{filteredBySql.ToString().ToLowerInvariant()}|{context.ChangeTracker.Entries().Count()}";
    }

    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new ExamContext(CreateOptions(connection));
        context.Database.EnsureCreated();
        context.Orders.AddRange(
            new Order { OrderId = 1, Total = 120.50m },
            new Order { OrderId = 2, Total = 75m },
            new Order { OrderId = 3, Total = 40.25m });
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
