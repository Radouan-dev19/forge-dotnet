using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class WeeklyPlans : Migration
{
    private static readonly string[] VersionIndexColumns =
        ["ProfileId", "DiagnosticSessionId", "Version"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WeeklyPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                DiagnosticSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CurriculumId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                CurriculumVersion = table.Column<int>(type: "INTEGER", nullable: false),
                CurriculumRevision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                TargetWeeklyHours = table.Column<int>(type: "INTEGER", nullable: false),
                PlanJson = table.Column<string>(type: "TEXT", maxLength: 262144, nullable: false),
                CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                AcceptedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WeeklyPlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_WeeklyPlans_DiagnosticEvaluations_DiagnosticSessionId",
                    column: x => x.DiagnosticSessionId,
                    principalTable: "DiagnosticEvaluations",
                    principalColumn: "SessionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WeeklyPlans_DiagnosticSessionId",
            table: "WeeklyPlans",
            column: "DiagnosticSessionId");

        migrationBuilder.CreateIndex(
            name: "IX_WeeklyPlans_ProfileId_DiagnosticSessionId_Version",
            table: "WeeklyPlans",
            columns: VersionIndexColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WeeklyPlans");
    }
}
