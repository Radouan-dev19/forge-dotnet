using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DiagnosticEvaluations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DiagnosticEvaluations",
            columns: table => new
            {
                SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                RubricId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                RubricVersion = table.Column<int>(type: "INTEGER", nullable: false),
                RubricRevision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                FrozenRubricJson = table.Column<string>(type: "TEXT", maxLength: 16384, nullable: false),
                ReportJson = table.Column<string>(type: "TEXT", maxLength: 65536, nullable: false),
                CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DiagnosticEvaluations", x => x.SessionId);
                table.ForeignKey(
                    name: "FK_DiagnosticEvaluations_DiagnosticSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "DiagnosticSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DiagnosticEvaluations");
    }
}
