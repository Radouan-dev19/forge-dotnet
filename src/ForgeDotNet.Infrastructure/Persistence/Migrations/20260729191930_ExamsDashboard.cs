using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ExamsDashboard : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExamAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                ExamId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ExamVersion = table.Column<int>(type: "INTEGER", nullable: false),
                ExamRevision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                PassingScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                DrawAlgorithm = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                DrawSeed = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                DrawCommitment = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                FrozenItemsJson = table.Column<string>(type: "TEXT", maxLength: 262144, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                StartedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                DeadlineUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                EndedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true),
                AssistanceDeclared = table.Column<bool>(type: "INTEGER", nullable: false),
                CompletionReason = table.Column<string>(type: "TEXT", maxLength: 24, nullable: true),
                ReportJson = table.Column<string>(type: "TEXT", maxLength: 262144, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExamAttempts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ExamSubmissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                ItemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                SourceFingerprint = table.Column<string>(type: "TEXT", maxLength: 71, nullable: false),
                SourceCode = table.Column<string>(type: "TEXT", maxLength: 64000, nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                TotalTests = table.Column<int>(type: "INTEGER", nullable: false),
                PassedTests = table.Column<int>(type: "INTEGER", nullable: false),
                HiddenFailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                DiagnosticId = table.Column<Guid>(type: "TEXT", nullable: false),
                SubmittedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExamSubmissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExamSubmissions_ExamAttempts_AttemptId",
                    column: x => x.AttemptId,
                    principalTable: "ExamAttempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ExamAttempts_ProfileId_StartedAtUtc",
            table: "ExamAttempts",
            columns: new[] { "ProfileId", "StartedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ExamAttempts_ProfileId_Status",
            table: "ExamAttempts",
            columns: new[] { "ProfileId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_ExamSubmissions_AttemptId_ItemId_Sequence",
            table: "ExamSubmissions",
            columns: new[] { "AttemptId", "ItemId", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExamSubmissions_DiagnosticId",
            table: "ExamSubmissions",
            column: "DiagnosticId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ExamSubmissions");

        migrationBuilder.DropTable(
            name: "ExamAttempts");
    }
}
