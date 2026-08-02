using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Mastery : Migration
{
    private static readonly string[] ProjectionIndexColumns = ["ProfileId", "PolicyRevision", "EvidenceRevision"];
    private static readonly string[] SqlAttemptIndexColumns = ["ProfileId", "ObservedAtUtc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MasteryProjections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                PolicyId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                PolicyVersion = table.Column<int>(type: "INTEGER", nullable: false),
                PolicyRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                EvidenceRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                FrozenPolicyJson = table.Column<string>(type: "TEXT", maxLength: 131072, nullable: false),
                SnapshotJson = table.Column<string>(type: "TEXT", maxLength: 262144, nullable: false),
                CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MasteryProjections", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SqlLearningAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                ScenarioId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ScenarioVersion = table.Column<int>(type: "INTEGER", nullable: false),
                ContentRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                ValidationRequested = table.Column<bool>(type: "INTEGER", nullable: false),
                ValidationPassed = table.Column<bool>(type: "INTEGER", nullable: true),
                QueryFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                DiagnosticId = table.Column<Guid>(type: "TEXT", nullable: false),
                ObservedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                ElapsedMilliseconds = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SqlLearningAttempts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MasteryProjections_ProfileId_PolicyRevision_EvidenceRevision",
            table: "MasteryProjections",
            columns: ProjectionIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SqlLearningAttempts_DiagnosticId",
            table: "SqlLearningAttempts",
            column: "DiagnosticId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SqlLearningAttempts_ProfileId_ObservedAtUtc",
            table: "SqlLearningAttempts",
            columns: SqlAttemptIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MasteryProjections");

        migrationBuilder.DropTable(
            name: "SqlLearningAttempts");
    }
}
