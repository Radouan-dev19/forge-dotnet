using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class LessonReaderState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LessonBookmarks",
            columns: table => new
            {
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                LessonId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                IsBookmarked = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LessonBookmarks", x => new { x.ProfileId, x.LessonId });
            });

        migrationBuilder.CreateTable(
            name: "LessonNotes",
            columns: table => new
            {
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                LessonId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                UpdatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LessonNotes", x => new { x.ProfileId, x.LessonId });
            });

        migrationBuilder.CreateTable(
            name: "LessonReadingActivities",
            columns: table => new
            {
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                LessonId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                ActivityId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                CompletedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LessonReadingActivities", x => new { x.ProfileId, x.LessonId, x.ActivityId });
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LessonBookmarks");

        migrationBuilder.DropTable(
            name: "LessonNotes");

        migrationBuilder.DropTable(
            name: "LessonReadingActivities");
    }
}
