using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Les tableaux sont générés par EF Core dans les appels CreateIndex.

namespace ForgeDotNet.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ReviewScheduling : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReviewItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                SourceKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                SourceKind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                SourceItemId = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                SourceItemVersion = table.Column<int>(type: "INTEGER", nullable: false),
                SourceRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                SourceOccurredAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                Domain = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ScheduleKind = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                Question = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ExpectedAnswer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                ChoicesJson = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: false),
                EvaluationMode = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                CanProduceMasteryEvidence = table.Column<bool>(type: "INTEGER", nullable: false),
                PolicyId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                PolicyVersion = table.Column<int>(type: "INTEGER", nullable: false),
                PolicyRevision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                CurrentIntervalIndex = table.Column<int>(type: "INTEGER", nullable: false),
                DueOn = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                LastReviewedAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReviewItems", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ReviewAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ReviewItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                IsMasteryEligible = table.Column<bool>(type: "INTEGER", nullable: false),
                Score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                ResponseFingerprint = table.Column<string>(type: "TEXT", maxLength: 71, nullable: false),
                PreviousDueOn = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                NextDueOn = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                NextIntervalDays = table.Column<int>(type: "INTEGER", nullable: false),
                AnsweredAtUtc = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReviewAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReviewAttempts_ReviewItems_ReviewItemId",
                    column: x => x.ReviewItemId,
                    principalTable: "ReviewItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReviewAttempts_ReviewItemId_Sequence",
            table: "ReviewAttempts",
            columns: new[] { "ReviewItemId", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ReviewItems_ProfileId_DueOn_IsActive",
            table: "ReviewItems",
            columns: new[] { "ProfileId", "DueOn", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_ReviewItems_ProfileId_SourceKey_SourceRevision_PolicyRevision",
            table: "ReviewItems",
            columns: new[] { "ProfileId", "SourceKey", "SourceRevision", "PolicyRevision" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ReviewAttempts");

        migrationBuilder.DropTable(
            name: "ReviewItems");
    }
}
