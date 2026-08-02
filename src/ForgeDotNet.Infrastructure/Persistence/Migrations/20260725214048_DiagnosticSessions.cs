using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DiagnosticSessions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DiagnosticSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                BankId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                BankVersion = table.Column<int>(type: "INTEGER", nullable: false),
                BankRevision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Mode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Seed = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CurrentSectionIndex = table.Column<int>(type: "INTEGER", nullable: false),
                SectionStatusesJson = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                FrozenPlanJson = table.Column<string>(type: "TEXT", maxLength: 131072, nullable: false),
                SectionDurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                StartedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                UpdatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                EndedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true),
                SectionStartedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true),
                SectionDeadlineUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DiagnosticSessions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DiagnosticResponses",
            columns: table => new
            {
                SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                QuestionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                SelectedOptionId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                SavedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DiagnosticResponses", x => new { x.SessionId, x.QuestionId });
                table.ForeignKey(
                    name: "FK_DiagnosticResponses_DiagnosticSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "DiagnosticSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DiagnosticSessions_ProfileId_StartedAtUtc",
            table: "DiagnosticSessions",
            columns: ["ProfileId", "StartedAtUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DiagnosticResponses");

        migrationBuilder.DropTable(
            name: "DiagnosticSessions");
    }
}
