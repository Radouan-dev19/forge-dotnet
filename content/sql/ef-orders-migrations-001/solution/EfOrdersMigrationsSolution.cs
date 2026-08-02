using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersMigrationsSolution
{
    public static Task ApplyAsync(MiniErpContext context, CancellationToken cancellationToken = default) =>
        context.Database.MigrateAsync(cancellationToken);
}

[DbContext(typeof(MiniErpContext))]
[Migration("202607290001_InitialMiniErp")]
public sealed class InitialMiniErpMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                CustomerId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Customers", item => item.CustomerId));

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                OrderId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                Total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", item => item.OrderId);
                table.ForeignKey(
                    name: "FK_Orders_Customers_CustomerId",
                    column: item => item.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "CustomerId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_Name",
            table: "Customers",
            column: "Name",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Orders_CustomerId_CreatedAt",
            table: "Orders",
            columns: ["CustomerId", "CreatedAtUtc"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Orders");
        migrationBuilder.DropTable(name: "Customers");
    }
}
