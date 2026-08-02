using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialLocalProfile : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LocalProfiles",
            columns: table => new
            {
                ProfileSlot = table.Column<int>(type: "INTEGER", nullable: false),
                LocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                ProfessionalGoal = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                WeeklyAvailableHours = table.Column<int>(type: "INTEGER", nullable: false),
                InterfaceLanguage = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                HasAcceptedLearningContract = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocalProfiles", x => x.ProfileSlot);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LocalProfiles_LocalId",
            table: "LocalProfiles",
            column: "LocalId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LocalProfiles");
    }
}
