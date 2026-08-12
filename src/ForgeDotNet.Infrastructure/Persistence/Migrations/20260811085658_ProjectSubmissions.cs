using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ProjectSubmissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProjectSubmissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ProjectVersion = table.Column<int>(type: "INTEGER", nullable: false),
                ContentRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                SubmissionFingerprint = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TotalSuites = table.Column<int>(type: "INTEGER", nullable: false),
                PassedSuites = table.Column<int>(type: "INTEGER", nullable: false),
                TotalTests = table.Column<int>(type: "INTEGER", nullable: false),
                PassedTests = table.Column<int>(type: "INTEGER", nullable: false),
                AutomaticallyVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                ObservedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectSubmissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProjectSubmissions_ProfileId_ObservedAtUtc",
            table: "ProjectSubmissions",
            columns: new[] { "ProfileId", "ObservedAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProjectSubmissions");
    }
}
