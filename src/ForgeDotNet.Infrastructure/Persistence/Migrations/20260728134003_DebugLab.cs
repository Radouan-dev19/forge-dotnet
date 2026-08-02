using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DebugLab : Migration
{
    private static readonly string[] AttemptIndexColumns = ["ActivityId", "Sequence"];
    private static readonly string[] ActivityIndexColumns = ["ProfileId", "ScenarioId"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DebugLabActivities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                ScenarioId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ScenarioVersion = table.Column<int>(type: "INTEGER", nullable: false),
                ContentRevision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                StartedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                Symptom = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Context = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Hypotheses = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Evidence = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Cause = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Fix = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Test = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Prevention = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                Breakpoint = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                Watch = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                Locals = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                CallStack = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                EvaluationJson = table.Column<string>(type: "TEXT", maxLength: 32768, nullable: true),
                SolutionViewedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true),
                CompletedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DebugLabActivities", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DebugCorrectionAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                SourceFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TotalTests = table.Column<int>(type: "INTEGER", nullable: false),
                PassedTests = table.Column<int>(type: "INTEGER", nullable: false),
                FailedTests = table.Column<int>(type: "INTEGER", nullable: false),
                DiagnosticId = table.Column<Guid>(type: "TEXT", nullable: false),
                SubmittedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DebugCorrectionAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_DebugCorrectionAttempts_DebugLabActivities_ActivityId",
                    column: x => x.ActivityId,
                    principalTable: "DebugLabActivities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DebugCorrectionAttempts_ActivityId_Sequence",
            table: "DebugCorrectionAttempts",
            columns: AttemptIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DebugLabActivities_ProfileId_ScenarioId",
            table: "DebugLabActivities",
            columns: ActivityIndexColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DebugCorrectionAttempts");

        migrationBuilder.DropTable(
            name: "DebugLabActivities");
    }
}
