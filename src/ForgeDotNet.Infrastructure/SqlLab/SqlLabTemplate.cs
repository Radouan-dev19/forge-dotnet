using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Infrastructure.SqlLab;

internal static class SqlLabTemplate
{
    public const string VisibleSchema = """
        dbo.Orders
          OrderId      int            NOT NULL  PRIMARY KEY
          CustomerName nvarchar(80)   NOT NULL
          Total        decimal(10,2)  NOT NULL
          CreatedAtUtc datetime2      NOT NULL
        """;

    public const string SchemaAndDatasetSql = """
        CREATE TABLE dbo.Orders
        (
            OrderId int NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
            CustomerName nvarchar(80) NOT NULL,
            Total decimal(10, 2) NOT NULL,
            CreatedAtUtc datetime2 NOT NULL
        );

        INSERT INTO dbo.Orders (OrderId, CustomerName, Total, CreatedAtUtc)
        VALUES
            (1, N'Ada', 120.50, '2026-01-10T09:00:00'),
            (2, N'Grace', 75.00, '2026-01-11T10:30:00'),
            (3, N'Linus', 40.25, '2026-01-12T14:15:00');
        """;

    public static SqlLabLimits CreateLimits(SqlLabOptions options) => new(
        options.QueryTimeoutSeconds,
        options.MaximumRows,
        options.MaximumResultBytes,
        options.MaximumQueryCharacters);
}
