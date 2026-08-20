using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class HumanAttestations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HumanAttestations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                TargetKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ReviewerName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ReviewerRelation = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                ReviewedOn = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                ArtifactDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                NamedGap = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ExplainedExerciseId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                CriteriaJson = table.Column<string>(type: "TEXT", maxLength: 16384, nullable: false),
                RecordedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HumanAttestations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HumanAttestations_ProfileId_TargetKey_ReviewedOn",
            table: "HumanAttestations",
            columns: new[] { "ProfileId", "TargetKey", "ReviewedOn" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HumanAttestations");
    }
}
