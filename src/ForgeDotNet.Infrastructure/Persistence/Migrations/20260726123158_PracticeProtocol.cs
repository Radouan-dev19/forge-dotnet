using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class PracticeProtocol : Migration
{
    private static readonly string[] ActivityProfileIndexColumns = ["ProfileId", "ExerciseId"];
    private static readonly string[] AttemptSequenceIndexColumns = ["ActivityId", "Sequence"];
    private static readonly string[] HintLevelIndexColumns = ["ActivityId", "Level"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PracticeActivities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                ExerciseId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ExerciseVersion = table.Column<int>(type: "INTEGER", nullable: false),
                ContentRevision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                StartedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                SolutionViewedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true),
                PersonalExplanation = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                VariantSubmission = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                PostSolutionCompletedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PracticeActivities", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PracticeAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                SubmissionText = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                ManualVerificationNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                ManualCheckDeclared = table.Column<bool>(type: "INTEGER", nullable: false),
                IsSerious = table.Column<bool>(type: "INTEGER", nullable: false),
                Decision = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                SubmissionFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                SubmittedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PracticeAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_PracticeAttempts_PracticeActivities_ActivityId",
                    column: x => x.ActivityId,
                    principalTable: "PracticeActivities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PracticeHintUsages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                Level = table.Column<int>(type: "INTEGER", nullable: false),
                Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                UsedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PracticeHintUsages", x => x.Id);
                table.ForeignKey(
                    name: "FK_PracticeHintUsages_PracticeActivities_ActivityId",
                    column: x => x.ActivityId,
                    principalTable: "PracticeActivities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PracticeReflections",
            columns: table => new
            {
                ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                Reformulation = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                Inputs = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                ExpectedOutput = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                EdgeCases = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                Hypothesis = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                Plan = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                UpdatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PracticeReflections", x => x.ActivityId);
                table.ForeignKey(
                    name: "FK_PracticeReflections_PracticeActivities_ActivityId",
                    column: x => x.ActivityId,
                    principalTable: "PracticeActivities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PracticeActivities_ProfileId_ExerciseId",
            table: "PracticeActivities",
            columns: ActivityProfileIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PracticeAttempts_ActivityId_Sequence",
            table: "PracticeAttempts",
            columns: AttemptSequenceIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PracticeHintUsages_ActivityId_Level",
            table: "PracticeHintUsages",
            columns: HintLevelIndexColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PracticeAttempts");

        migrationBuilder.DropTable(
            name: "PracticeHintUsages");

        migrationBuilder.DropTable(
            name: "PracticeReflections");

        migrationBuilder.DropTable(
            name: "PracticeActivities");
    }
}
