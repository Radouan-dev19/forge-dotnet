using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class Submission
{
    public static string Run(decimal minimumTotal)
    {
        using SqliteConnection connection = CreateDatabase();
        var commandCapture = new CommandCaptureInterceptor();
        using var context = new ExamContext(CreateOptions(connection, commandCapture));

        // Corrigez la requête : elle ignore encore minimumTotal et le mode lecture seule.
        IQueryable<int> query = context.Orders
            .OrderBy(item => item.OrderId)
            .Select(item => item.OrderId);

        string ids = string.Join(',', query.ToArray());
        string sql = commandCapture.LastCommandText ?? string.Empty;
        bool filteredBySql = sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase);
        return $"{ids}|{filteredBySql.ToString().ToLowerInvariant()}|{context.ChangeTracker.Entries().Count()}";
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
                (2, '75'),
                (3, '40.25');
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static DbContextOptions<ExamContext> CreateOptions(
        SqliteConnection connection,
        DbCommandInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ExamContext>().UseSqlite(connection);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return builder.Options;
    }

    private sealed class ExamContext(DbContextOptions<ExamContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
    }

    private sealed class Order
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public string? LastCommandText { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            LastCommandText = command.CommandText;
            return result;
        }
    }
}
