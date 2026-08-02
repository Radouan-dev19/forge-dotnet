using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class PracticeLearningObservations : Migration
{
    private static readonly string[] AttemptIndexColumns = ["ProfileId", "ObservedAtUtc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PracticeLearningAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                ExerciseId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ExerciseVersion = table.Column<int>(type: "INTEGER", nullable: false),
                ContentRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                SubmissionFingerprint = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TotalTests = table.Column<int>(type: "INTEGER", nullable: false),
                PassedTests = table.Column<int>(type: "INTEGER", nullable: false),
                DiagnosticId = table.Column<Guid>(type: "TEXT", nullable: false),
                ObservedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PracticeLearningAttempts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PracticeLearningAttempts_DiagnosticId",
            table: "PracticeLearningAttempts",
            column: "DiagnosticId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PracticeLearningAttempts_ProfileId_ObservedAtUtc",
            table: "PracticeLearningAttempts",
            columns: AttemptIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PracticeLearningAttempts");
    }
}
