using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class ForgeDbContext(DbContextOptions<ForgeDbContext> options) : DbContext(options)
{
    internal DbSet<LocalProfileRecord> LocalProfiles => Set<LocalProfileRecord>();

    internal DbSet<LessonNoteRecord> LessonNotes => Set<LessonNoteRecord>();

    internal DbSet<LessonBookmarkRecord> LessonBookmarks => Set<LessonBookmarkRecord>();

    internal DbSet<LessonReadingActivityRecord> LessonReadingActivities => Set<LessonReadingActivityRecord>();

    internal DbSet<DiagnosticSessionRecord> DiagnosticSessions => Set<DiagnosticSessionRecord>();

    internal DbSet<DiagnosticResponseRecord> DiagnosticResponses => Set<DiagnosticResponseRecord>();

    internal DbSet<DiagnosticEvaluationRecord> DiagnosticEvaluations => Set<DiagnosticEvaluationRecord>();

    internal DbSet<WeeklyPlanRecord> WeeklyPlans => Set<WeeklyPlanRecord>();

    internal DbSet<PracticeActivityRecord> PracticeActivities => Set<PracticeActivityRecord>();

    internal DbSet<PracticeReflectionRecord> PracticeReflections => Set<PracticeReflectionRecord>();

    internal DbSet<PracticeAttemptRecord> PracticeAttempts => Set<PracticeAttemptRecord>();

    internal DbSet<PracticeHintUsageRecord> PracticeHintUsages => Set<PracticeHintUsageRecord>();

    internal DbSet<DebugLabActivityRecord> DebugLabActivities => Set<DebugLabActivityRecord>();

    internal DbSet<DebugCorrectionAttemptRecord> DebugCorrectionAttempts => Set<DebugCorrectionAttemptRecord>();

    internal DbSet<PracticeLearningAttemptRecord> PracticeLearningAttempts => Set<PracticeLearningAttemptRecord>();

    internal DbSet<SqlLearningAttemptRecord> SqlLearningAttempts => Set<SqlLearningAttemptRecord>();

    internal DbSet<MasteryProjectionRecord> MasteryProjections => Set<MasteryProjectionRecord>();

    internal DbSet<ReviewItemRecord> ReviewItems => Set<ReviewItemRecord>();

    internal DbSet<ReviewAttemptRecord> ReviewAttempts => Set<ReviewAttemptRecord>();

    internal DbSet<ExamAttemptRecord> ExamAttempts => Set<ExamAttemptRecord>();

    internal DbSet<ExamSubmissionRecord> ExamSubmissions => Set<ExamSubmissionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<LocalProfileRecord>();
        profile.ToTable("LocalProfiles");
        profile.HasKey(item => item.ProfileSlot);
        profile.Property(item => item.ProfileSlot).ValueGeneratedNever();
        profile.HasIndex(item => item.LocalId).IsUnique();
        profile.Property(item => item.DisplayName).HasMaxLength(80).IsRequired();
        profile.Property(item => item.ProfessionalGoal).HasMaxLength(300).IsRequired();
        profile.Property(item => item.InterfaceLanguage).HasConversion<string>().HasMaxLength(16).IsRequired();
        profile.Property(item => item.CreatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        profile.Property(item => item.WeeklyAvailableHours).IsRequired();
        profile.Property(item => item.HasAcceptedLearningContract).IsRequired();

        var note = modelBuilder.Entity<LessonNoteRecord>();
        note.ToTable("LessonNotes");
        note.HasKey(item => new { item.ProfileId, item.LessonId });
        note.Property(item => item.LessonId).HasMaxLength(120).IsRequired();
        note.Property(item => item.Text).HasMaxLength(4_000).IsRequired();
        note.Property(item => item.UpdatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();

        var bookmark = modelBuilder.Entity<LessonBookmarkRecord>();
        bookmark.ToTable("LessonBookmarks");
        bookmark.HasKey(item => new { item.ProfileId, item.LessonId });
        bookmark.Property(item => item.LessonId).HasMaxLength(120).IsRequired();
        bookmark.Property(item => item.IsBookmarked).IsRequired();
        bookmark.Property(item => item.UpdatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();

        var activity = modelBuilder.Entity<LessonReadingActivityRecord>();
        activity.ToTable("LessonReadingActivities");
        activity.HasKey(item => new { item.ProfileId, item.LessonId, item.ActivityId });
        activity.Property(item => item.LessonId).HasMaxLength(120).IsRequired();
        activity.Property(item => item.ActivityId).HasMaxLength(160).IsRequired();
        activity.Property(item => item.CompletedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();

        var diagnosticSession = modelBuilder.Entity<DiagnosticSessionRecord>();
        diagnosticSession.ToTable("DiagnosticSessions");
        diagnosticSession.HasKey(item => item.Id);
        diagnosticSession.Property(item => item.Id).ValueGeneratedNever();
        diagnosticSession.HasIndex(item => new { item.ProfileId, item.StartedAtUtc });
        diagnosticSession.Property(item => item.BankId).HasMaxLength(80).IsRequired();
        diagnosticSession.Property(item => item.BankRevision).HasMaxLength(64).IsRequired();
        diagnosticSession.Property(item => item.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
        diagnosticSession.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        diagnosticSession.Property(item => item.SectionStatusesJson).HasMaxLength(512).IsRequired();
        diagnosticSession.Property(item => item.FrozenPlanJson).HasMaxLength(131_072).IsRequired();
        diagnosticSession.Property(item => item.StartedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        diagnosticSession.Property(item => item.UpdatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        diagnosticSession.Property(item => item.EndedAtUtc).HasConversion<string>().HasMaxLength(48);
        diagnosticSession.Property(item => item.SectionStartedAtUtc).HasConversion<string>().HasMaxLength(48);
        diagnosticSession.Property(item => item.SectionDeadlineUtc).HasConversion<string>().HasMaxLength(48);

        var diagnosticResponse = modelBuilder.Entity<DiagnosticResponseRecord>();
        diagnosticResponse.ToTable("DiagnosticResponses");
        diagnosticResponse.HasKey(item => new { item.SessionId, item.QuestionId });
        diagnosticResponse.Property(item => item.QuestionId).HasMaxLength(100).IsRequired();
        diagnosticResponse.Property(item => item.SelectedOptionId).HasMaxLength(32).IsRequired();
        diagnosticResponse.Property(item => item.SavedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        diagnosticResponse.HasOne<DiagnosticSessionRecord>()
            .WithMany()
            .HasForeignKey(item => item.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        var diagnosticEvaluation = modelBuilder.Entity<DiagnosticEvaluationRecord>();
        diagnosticEvaluation.ToTable("DiagnosticEvaluations");
        diagnosticEvaluation.HasKey(item => item.SessionId);
        diagnosticEvaluation.Property(item => item.RubricId).HasMaxLength(80).IsRequired();
        diagnosticEvaluation.Property(item => item.RubricRevision).HasMaxLength(64).IsRequired();
        diagnosticEvaluation.Property(item => item.FrozenRubricJson).HasMaxLength(16_384).IsRequired();
        diagnosticEvaluation.Property(item => item.ReportJson).HasMaxLength(65_536).IsRequired();
        diagnosticEvaluation.Property(item => item.CreatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        diagnosticEvaluation.HasOne<DiagnosticSessionRecord>()
            .WithOne()
            .HasForeignKey<DiagnosticEvaluationRecord>(item => item.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        var weeklyPlan = modelBuilder.Entity<WeeklyPlanRecord>();
        weeklyPlan.ToTable("WeeklyPlans");
        weeklyPlan.HasKey(item => item.Id);
        weeklyPlan.Property(item => item.Id).ValueGeneratedNever();
        weeklyPlan.HasIndex(item => new { item.ProfileId, item.DiagnosticSessionId, item.Version }).IsUnique();
        weeklyPlan.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        weeklyPlan.Property(item => item.CurriculumId).HasMaxLength(80).IsRequired();
        weeklyPlan.Property(item => item.CurriculumRevision).HasMaxLength(64).IsRequired();
        weeklyPlan.Property(item => item.PlanJson).HasMaxLength(262_144).IsRequired();
        weeklyPlan.Property(item => item.CreatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        weeklyPlan.Property(item => item.AcceptedAtUtc).HasConversion<string>().HasMaxLength(48);
        weeklyPlan.HasOne<DiagnosticEvaluationRecord>()
            .WithMany()
            .HasForeignKey(item => item.DiagnosticSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        var practiceActivity = modelBuilder.Entity<PracticeActivityRecord>();
        practiceActivity.ToTable("PracticeActivities");
        practiceActivity.HasKey(item => item.Id);
        practiceActivity.Property(item => item.Id).ValueGeneratedNever();
        practiceActivity.HasIndex(item => new { item.ProfileId, item.ExerciseId }).IsUnique();
        practiceActivity.Property(item => item.ExerciseId).HasMaxLength(128).IsRequired();
        practiceActivity.Property(item => item.ContentRevision).HasMaxLength(64).IsRequired();
        practiceActivity.Property(item => item.State).HasConversion<string>().HasMaxLength(32).IsRequired();
        practiceActivity.Property(item => item.StartedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        practiceActivity.Property(item => item.SolutionViewedAtUtc).HasConversion<string>().HasMaxLength(48);
        practiceActivity.Property(item => item.PersonalExplanation).HasMaxLength(8_000);
        practiceActivity.Property(item => item.VariantSubmission).HasMaxLength(8_000);
        practiceActivity.Property(item => item.PostSolutionCompletedAtUtc).HasConversion<string>().HasMaxLength(48);

        var practiceReflection = modelBuilder.Entity<PracticeReflectionRecord>();
        practiceReflection.ToTable("PracticeReflections");
        practiceReflection.HasKey(item => item.ActivityId);
        practiceReflection.Property(item => item.Reformulation).HasMaxLength(4_000).IsRequired();
        practiceReflection.Property(item => item.Inputs).HasMaxLength(4_000).IsRequired();
        practiceReflection.Property(item => item.ExpectedOutput).HasMaxLength(4_000).IsRequired();
        practiceReflection.Property(item => item.EdgeCases).HasMaxLength(4_000).IsRequired();
        practiceReflection.Property(item => item.Hypothesis).HasMaxLength(4_000).IsRequired();
        practiceReflection.Property(item => item.Plan).HasMaxLength(4_000).IsRequired();
        practiceReflection.Property(item => item.UpdatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        practiceReflection.HasOne<PracticeActivityRecord>()
            .WithOne()
            .HasForeignKey<PracticeReflectionRecord>(item => item.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        var practiceAttempt = modelBuilder.Entity<PracticeAttemptRecord>();
        practiceAttempt.ToTable("PracticeAttempts");
        practiceAttempt.HasKey(item => item.Id);
        practiceAttempt.Property(item => item.Id).ValueGeneratedNever();
        practiceAttempt.HasIndex(item => new { item.ActivityId, item.Sequence }).IsUnique();
        practiceAttempt.Property(item => item.SubmissionText).HasMaxLength(20_000).IsRequired();
        practiceAttempt.Property(item => item.ManualVerificationNotes).HasMaxLength(4_000).IsRequired();
        practiceAttempt.Property(item => item.Decision).HasConversion<string>().HasMaxLength(40).IsRequired();
        practiceAttempt.Property(item => item.SubmissionFingerprint).HasMaxLength(64).IsRequired();
        practiceAttempt.Property(item => item.SubmittedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        practiceAttempt.HasOne<PracticeActivityRecord>()
            .WithMany()
            .HasForeignKey(item => item.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        var practiceHint = modelBuilder.Entity<PracticeHintUsageRecord>();
        practiceHint.ToTable("PracticeHintUsages");
        practiceHint.HasKey(item => item.Id);
        practiceHint.Property(item => item.Id).ValueGeneratedNever();
        practiceHint.HasIndex(item => new { item.ActivityId, item.Level }).IsUnique();
        practiceHint.Property(item => item.Kind).HasMaxLength(32).IsRequired();
        practiceHint.Property(item => item.UsedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        practiceHint.HasOne<PracticeActivityRecord>()
            .WithMany()
            .HasForeignKey(item => item.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        var debugActivity = modelBuilder.Entity<DebugLabActivityRecord>();
        debugActivity.ToTable("DebugLabActivities");
        debugActivity.HasKey(item => item.Id);
        debugActivity.Property(item => item.Id).ValueGeneratedNever();
        debugActivity.HasIndex(item => new { item.ProfileId, item.ScenarioId }).IsUnique();
        debugActivity.Property(item => item.ScenarioId).HasMaxLength(128).IsRequired();
        debugActivity.Property(item => item.ContentRevision).HasMaxLength(64).IsRequired();
        debugActivity.Property(item => item.State).HasConversion<string>().HasMaxLength(32).IsRequired();
        debugActivity.Property(item => item.StartedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        debugActivity.Property(item => item.Symptom).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Context).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Hypotheses).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Evidence).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Cause).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Fix).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Test).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Prevention).HasMaxLength(8_000).IsRequired();
        debugActivity.Property(item => item.Breakpoint).HasMaxLength(8_000);
        debugActivity.Property(item => item.Watch).HasMaxLength(8_000);
        debugActivity.Property(item => item.Locals).HasMaxLength(8_000);
        debugActivity.Property(item => item.CallStack).HasMaxLength(8_000);
        debugActivity.Property(item => item.EvaluationJson).HasMaxLength(32_768);
        debugActivity.Property(item => item.SolutionViewedAtUtc).HasConversion<string>().HasMaxLength(48);
        debugActivity.Property(item => item.CompletedAtUtc).HasConversion<string>().HasMaxLength(48);

        var debugAttempt = modelBuilder.Entity<DebugCorrectionAttemptRecord>();
        debugAttempt.ToTable("DebugCorrectionAttempts");
        debugAttempt.HasKey(item => item.Id);
        debugAttempt.Property(item => item.Id).ValueGeneratedNever();
        debugAttempt.HasIndex(item => new { item.ActivityId, item.Sequence }).IsUnique();
        debugAttempt.Property(item => item.SourceFingerprint).HasMaxLength(64).IsRequired();
        debugAttempt.Property(item => item.Outcome).HasConversion<string>().HasMaxLength(32).IsRequired();
        debugAttempt.Property(item => item.SubmittedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        debugAttempt.HasOne<DebugLabActivityRecord>()
            .WithMany()
            .HasForeignKey(item => item.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        var practiceLearningAttempt = modelBuilder.Entity<PracticeLearningAttemptRecord>();
        practiceLearningAttempt.ToTable("PracticeLearningAttempts");
        practiceLearningAttempt.HasKey(item => item.Id);
        practiceLearningAttempt.Property(item => item.Id).ValueGeneratedNever();
        practiceLearningAttempt.HasIndex(item => item.DiagnosticId).IsUnique();
        practiceLearningAttempt.HasIndex(item => new { item.ProfileId, item.ObservedAtUtc });
        practiceLearningAttempt.Property(item => item.ExerciseId).HasMaxLength(100).IsRequired();
        practiceLearningAttempt.Property(item => item.ContentRevision).HasMaxLength(80).IsRequired();
        practiceLearningAttempt.Property(item => item.SubmissionFingerprint).HasMaxLength(80).IsRequired();
        practiceLearningAttempt.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        practiceLearningAttempt.Property(item => item.ObservedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();

        var sqlAttempt = modelBuilder.Entity<SqlLearningAttemptRecord>();
        sqlAttempt.ToTable("SqlLearningAttempts");
        sqlAttempt.HasKey(item => item.Id);
        sqlAttempt.Property(item => item.Id).ValueGeneratedNever();
        sqlAttempt.HasIndex(item => item.DiagnosticId).IsUnique();
        sqlAttempt.HasIndex(item => new { item.ProfileId, item.ObservedAtUtc });
        sqlAttempt.Property(item => item.ScenarioId).HasMaxLength(128).IsRequired();
        sqlAttempt.Property(item => item.ContentRevision).HasMaxLength(80).IsRequired();
        sqlAttempt.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        sqlAttempt.Property(item => item.QueryFingerprint).HasMaxLength(80).IsRequired();
        sqlAttempt.Property(item => item.ObservedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();

        var masteryProjection = modelBuilder.Entity<MasteryProjectionRecord>();
        masteryProjection.ToTable("MasteryProjections");
        masteryProjection.HasKey(item => item.Id);
        masteryProjection.Property(item => item.Id).ValueGeneratedNever();
        masteryProjection.HasIndex(item => new
        {
            item.ProfileId,
            item.PolicyRevision,
            item.EvidenceRevision,
        }).IsUnique();
        masteryProjection.Property(item => item.PolicyId).HasMaxLength(80).IsRequired();
        masteryProjection.Property(item => item.PolicyRevision).HasMaxLength(80).IsRequired();
        masteryProjection.Property(item => item.EvidenceRevision).HasMaxLength(80).IsRequired();
        masteryProjection.Property(item => item.FrozenPolicyJson).HasMaxLength(131_072).IsRequired();
        masteryProjection.Property(item => item.SnapshotJson).HasMaxLength(262_144).IsRequired();
        masteryProjection.Property(item => item.CreatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();

        var reviewItem = modelBuilder.Entity<ReviewItemRecord>();
        reviewItem.ToTable("ReviewItems");
        reviewItem.HasKey(item => item.Id);
        reviewItem.Property(item => item.Id).ValueGeneratedNever();
        reviewItem.HasIndex(item => new
        {
            item.ProfileId,
            item.SourceKey,
            item.SourceRevision,
            item.PolicyRevision,
        }).IsUnique();
        reviewItem.HasIndex(item => new { item.ProfileId, item.DueOn, item.IsActive });
        reviewItem.Property(item => item.SourceKey).HasMaxLength(200).IsRequired();
        reviewItem.Property(item => item.SourceKind).HasConversion<string>().HasMaxLength(40).IsRequired();
        reviewItem.Property(item => item.SourceItemId).HasMaxLength(160).IsRequired();
        reviewItem.Property(item => item.SourceRevision).HasMaxLength(80).IsRequired();
        reviewItem.Property(item => item.SourceOccurredAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        reviewItem.Property(item => item.Domain).HasConversion<string>().HasMaxLength(40).IsRequired();
        reviewItem.Property(item => item.ScheduleKind).HasConversion<string>().HasMaxLength(24).IsRequired();
        reviewItem.Property(item => item.Question).HasMaxLength(2_000).IsRequired();
        reviewItem.Property(item => item.ExpectedAnswer).HasMaxLength(2_000);
        reviewItem.Property(item => item.ChoicesJson).HasMaxLength(8_192).IsRequired();
        reviewItem.Property(item => item.EvaluationMode).HasConversion<string>().HasMaxLength(24).IsRequired();
        reviewItem.Property(item => item.PolicyId).HasMaxLength(80).IsRequired();
        reviewItem.Property(item => item.PolicyRevision).HasMaxLength(80).IsRequired();
        reviewItem.Property(item => item.DueOn).HasConversion<string>().HasMaxLength(16).IsRequired();
        reviewItem.Property(item => item.Version).IsConcurrencyToken();
        reviewItem.Property(item => item.CreatedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        reviewItem.Property(item => item.LastReviewedAtUtc).HasConversion<string>().HasMaxLength(48);

        var reviewAttempt = modelBuilder.Entity<ReviewAttemptRecord>();
        reviewAttempt.ToTable("ReviewAttempts");
        reviewAttempt.HasKey(item => item.Id);
        reviewAttempt.Property(item => item.Id).ValueGeneratedNever();
        reviewAttempt.HasIndex(item => new { item.ReviewItemId, item.Sequence }).IsUnique();
        reviewAttempt.Property(item => item.Outcome).HasConversion<string>().HasMaxLength(16).IsRequired();
        reviewAttempt.Property(item => item.Score).HasPrecision(5, 2);
        reviewAttempt.Property(item => item.ResponseFingerprint).HasMaxLength(71).IsRequired();
        reviewAttempt.Property(item => item.PreviousDueOn).HasConversion<string>().HasMaxLength(16).IsRequired();
        reviewAttempt.Property(item => item.NextDueOn).HasConversion<string>().HasMaxLength(16).IsRequired();
        reviewAttempt.Property(item => item.AnsweredAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        reviewAttempt.HasOne<ReviewItemRecord>()
            .WithMany()
            .HasForeignKey(item => item.ReviewItemId)
            .OnDelete(DeleteBehavior.Cascade);

        var examAttempt = modelBuilder.Entity<ExamAttemptRecord>();
        examAttempt.ToTable("ExamAttempts");
        examAttempt.HasKey(item => item.Id);
        examAttempt.Property(item => item.Id).ValueGeneratedNever();
        examAttempt.HasIndex(item => new { item.ProfileId, item.StartedAtUtc });
        examAttempt.HasIndex(item => new { item.ProfileId, item.Status });
        examAttempt.Property(item => item.ExamId).HasMaxLength(100).IsRequired();
        examAttempt.Property(item => item.ExamRevision).HasMaxLength(64).IsRequired();
        examAttempt.Property(item => item.Title).HasMaxLength(160).IsRequired();
        examAttempt.Property(item => item.PassingScore).HasPrecision(5, 2);
        examAttempt.Property(item => item.DrawAlgorithm).HasMaxLength(32).IsRequired();
        examAttempt.Property(item => item.DrawSeed).HasMaxLength(64).IsRequired();
        examAttempt.Property(item => item.DrawCommitment).HasMaxLength(64).IsRequired();
        examAttempt.Property(item => item.FrozenItemsJson).HasMaxLength(262_144).IsRequired();
        examAttempt.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        examAttempt.Property(item => item.Version).IsConcurrencyToken();
        examAttempt.Property(item => item.StartedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        examAttempt.Property(item => item.DeadlineUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        examAttempt.Property(item => item.EndedAtUtc).HasConversion<string>().HasMaxLength(48);
        examAttempt.Property(item => item.CompletionReason).HasConversion<string>().HasMaxLength(24);
        examAttempt.Property(item => item.ReportJson).HasMaxLength(262_144);

        var examSubmission = modelBuilder.Entity<ExamSubmissionRecord>();
        examSubmission.ToTable("ExamSubmissions");
        examSubmission.HasKey(item => item.Id);
        examSubmission.Property(item => item.Id).ValueGeneratedNever();
        examSubmission.HasIndex(item => item.DiagnosticId).IsUnique();
        examSubmission.HasIndex(item => new { item.AttemptId, item.ItemId, item.Sequence }).IsUnique();
        examSubmission.Property(item => item.ItemId).HasMaxLength(100).IsRequired();
        examSubmission.Property(item => item.SourceFingerprint).HasMaxLength(71).IsRequired();
        examSubmission.Property(item => item.SourceCode).HasMaxLength(64_000).IsRequired();
        examSubmission.Property(item => item.Outcome).HasConversion<string>().HasMaxLength(24).IsRequired();
        examSubmission.Property(item => item.SubmittedAtUtc).HasConversion<string>().HasMaxLength(48).IsRequired();
        examSubmission.HasOne<ExamAttemptRecord>()
            .WithMany()
            .HasForeignKey(item => item.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
