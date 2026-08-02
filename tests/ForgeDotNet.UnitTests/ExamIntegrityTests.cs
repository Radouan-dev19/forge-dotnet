using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Domain.Analytics;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.UnitTests;

[Trait("Category", "ExamIntegrity")]
public sealed class ExamIntegrityTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DrawIsDeterministicAuditableAndChangesCommitmentWithSeed()
    {
        ExamBlueprint blueprint = Blueprint();
        byte[] firstSeed = Enumerable.Range(0, 32).Select(index => (byte)index).ToArray();
        byte[] secondSeed = Enumerable.Range(1, 32).Select(index => (byte)index).ToArray();

        ExamAttempt first = ExamRules.Start(Guid.NewGuid(), Guid.NewGuid(), blueprint, firstSeed, Now);
        ExamAttempt replay = ExamRules.Start(Guid.NewGuid(), first.ProfileId, blueprint, firstSeed, Now);
        ExamAttempt different = ExamRules.Start(Guid.NewGuid(), first.ProfileId, blueprint, secondSeed, Now);

        Assert.Equal(first.Items.Select(item => item.ItemId), replay.Items.Select(item => item.ItemId));
        Assert.Equal(first.DrawCommitment, replay.DrawCommitment);
        Assert.NotEqual(first.DrawCommitment, different.DrawCommitment);
        string recomputed = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{ExamRules.DrawAlgorithm}|{first.DrawSeed}")));
        Assert.Equal(recomputed, first.DrawCommitment);
        Assert.Equal(first.StartedAtUtc.AddMinutes(blueprint.DurationMinutes), first.DeadlineUtc);
    }

    [Fact]
    public void ServerDeadlineControlsResumeAndSubmission()
    {
        ExamAttempt attempt = ExamRules.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Blueprint(),
            new byte[32],
            Now);

        Assert.True(ExamRules.CanResume(attempt, attempt.DeadlineUtc.AddTicks(-1)));
        Assert.False(ExamRules.CanResume(attempt, attempt.DeadlineUtc));
        Assert.Throws<InvalidOperationException>(() =>
            ExamRules.RecordSubmission(attempt, attempt.Version, attempt.DeadlineUtc));
    }

    [Fact]
    public void PersistedItemWithUnknownSubmissionKindIsRefused()
    {
        ExamAttempt attempt = ExamRules.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Blueprint(),
            new byte[32],
            Now);
        ExamItemSnapshot[] corruptedItems = attempt.Items
            .Select((item, index) => index == 0
                ? item with { SubmissionKind = (ExamSubmissionKind)999 }
                : item)
            .ToArray();

        Assert.Throws<InvalidDataException>(() => ExamRules.ValidateAttempt(
            attempt with { Items = Array.AsReadOnly(corruptedItems) }));
    }

    [Fact]
    public void ReportIsFrozenOnlyAfterFinishAndDoubleFinishIsRefused()
    {
        ExamAttempt attempt = ExamRules.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Blueprint(),
            new byte[32],
            Now);
        ExamItemSnapshot item = attempt.Items[0];
        var submission = new ExamSubmission(
            Guid.NewGuid(),
            attempt.Id,
            item.ItemId,
            1,
            $"sha256:{new string('a', 64)}",
            "public static class Submission { public static int Run() => 1; }",
            ExamSubmissionOutcome.Succeeded,
            4,
            4,
            0,
            Guid.NewGuid(),
            Now.AddMinutes(5));
        attempt = ExamRules.RecordSubmission(attempt, 1, submission.SubmittedAtUtc);

        ExamCompletion completion = ExamRules.Finish(
            attempt,
            attempt.Version,
            [submission],
            ExamCompletionReason.Submitted,
            assistanceDeclared: false,
            Now.AddMinutes(10));

        Assert.Equal(ExamAttemptStatus.Completed, completion.Attempt.Status);
        Assert.Equal(attempt.Version + 1, completion.Attempt.Version);
        Assert.Equal(50m, completion.Report.Score);
        Assert.False(completion.Report.Passed);
        Assert.Equal(attempt.DrawSeed, completion.Report.DrawSeed);
        Assert.Throws<InvalidOperationException>(() => ExamRules.Finish(
            completion.Attempt,
            completion.Attempt.Version,
            [submission],
            ExamCompletionReason.Submitted,
            assistanceDeclared: false,
            Now.AddMinutes(11)));
    }

    [Fact]
    public void AbandonAndDeclaredAssistanceNeverPass()
    {
        ExamAttempt attempt = ExamRules.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Blueprint(),
            new byte[32],
            Now);
        ExamCompletion abandoned = ExamRules.Finish(
            attempt,
            1,
            [],
            ExamCompletionReason.Abandoned,
            assistanceDeclared: false,
            Now.AddMinutes(1));

        Assert.Equal(ExamAttemptStatus.Abandoned, abandoned.Attempt.Status);
        Assert.False(abandoned.Report.Passed);

        ExamAttempt assistedAttempt = ExamRules.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Blueprint(drawCount: 1),
            new byte[32],
            Now);
        ExamItemSnapshot item = assistedAttempt.Items[0];
        var passed = new ExamSubmission(
            Guid.NewGuid(), assistedAttempt.Id, item.ItemId, 1, $"sha256:{new string('b', 64)}", "valid source",
            ExamSubmissionOutcome.Succeeded, 2, 2, 0, Guid.NewGuid(), Now.AddMinutes(2));
        assistedAttempt = ExamRules.RecordSubmission(assistedAttempt, 1, passed.SubmittedAtUtc);
        ExamCompletion assisted = ExamRules.Finish(
            assistedAttempt,
            2,
            [passed],
            ExamCompletionReason.Submitted,
            assistanceDeclared: true,
            Now.AddMinutes(3));

        Assert.Equal(100m, assisted.Report.Score);
        Assert.False(assisted.Report.Passed);
    }

    [Fact]
    public void AnalyticsExcludesInactiveGapsAndNeverInventsUnavailableRates()
    {
        var evidence = new AnalyticsEvidence(
            [
                new("practice:a", Now),
                new("practice:a", Now.AddMinutes(2)),
                new("practice:a", Now.AddMinutes(12)),
                new("practice:b", Now.AddMinutes(20)),
            ],
            [],
            [],
            HintUsageCount: 0,
            SolutionViewCount: 0,
            NextObjective: null);

        AnalyticsSnapshot snapshot = AnalyticsRules.Calculate(evidence, TimeSpan.FromMinutes(5), Now.AddHours(1));

        Assert.Equal(2, snapshot.ObservedActiveMinutes);
        Assert.Equal(1, snapshot.ActiveIntervalCount);
        Assert.Null(snapshot.FirstAttemptSuccessRate);
        Assert.Null(snapshot.BeforeSolutionSuccessRate);
        Assert.Null(snapshot.AverageExamScore);
    }

    [Fact]
    public void AnalyticsUsesFirstAttemptAndPreservesAssistanceFacts()
    {
        var evidence = new AnalyticsEvidence(
            [],
            [
                new("practice:a", 1, Passed: false, SolutionViewedBefore: false, 0, Now),
                new("practice:a", 2, Passed: true, SolutionViewedBefore: false, 0, Now.AddMinutes(1)),
                new("practice:b", 1, Passed: true, SolutionViewedBefore: true, 3, Now.AddMinutes(2)),
            ],
            [
                new(AnalyticsExamStatus.Completed, 80m, Now),
                new(AnalyticsExamStatus.Abandoned, 0m, Now),
            ],
            HintUsageCount: 3,
            SolutionViewCount: 1,
            NextObjective: "Semaine 1 — C#");

        AnalyticsSnapshot snapshot = AnalyticsRules.Calculate(evidence, TimeSpan.FromMinutes(5), Now.AddHours(1));

        Assert.Equal(50m, snapshot.FirstAttemptSuccessRate);
        Assert.Equal(0m, snapshot.BeforeSolutionSuccessRate);
        Assert.Equal(3, snapshot.HintUsageCount);
        Assert.Equal(1, snapshot.SolutionViewCount);
        Assert.Equal(1, snapshot.CompletedExamCount);
        Assert.Equal(1, snapshot.AbandonedExamCount);
        Assert.Equal(80m, snapshot.AverageExamScore);
    }

    [Fact]
    public void PerfectExamCannotCompensateCriticalRequirementsOrOpenGate()
    {
        Guid profileId = Guid.NewGuid();
        var exam = new MasteryObservation(
            Guid.NewGuid(),
            profileId,
            MasteryDomain.CSharp,
            MasteryComponent.UnassistedExam,
            MasteryEvidenceSource.Exam,
            MasteryVerificationKind.ExamEngine,
            "exam-item",
            1,
            new string('c', 64),
            100m,
            MasteryAssistance.None,
            Now,
            "exam:test");
        var evidence = new MasteryEvidenceSet([exam], [], $"sha256:{new string('d', 64)}");

        MasterySnapshot snapshot = MasteryRules.Calculate(
            profileId,
            MasteryPolicyCatalog.Version1,
            evidence,
            Now.AddMinutes(1));
        MasteryDomainScore csharp = snapshot.Domains.Single(item => item.Domain == MasteryDomain.CSharp);

        Assert.True(csharp.IsCritical);
        Assert.Equal(25m, csharp.Score);
        Assert.False(csharp.IsValidated);
        Assert.False(snapshot.Gates.Single(item => item.Gate == MasteryGate.A).IsOpen);
    }

    private static ExamBlueprint Blueprint(int drawCount = 2) => new(
        "reference-exam-v1",
        1,
        new string('e', 64),
        "Examen de référence",
        30,
        drawCount,
        70m,
        Array.AsReadOnly(Enumerable.Range(1, 4).Select(index => new ExamCandidate(
            $"exam-item-{index}",
            1,
            index.ToString("x", CultureInfo.InvariantCulture).PadLeft(64, '0'),
            MasteryDomain.CSharp,
            $"Item {index}",
            "Résoudre le problème sans aide.",
            Array.Empty<string>(),
            "Submission.cs",
            "public static class Submission { }")).ToArray()));
}
