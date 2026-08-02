using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.UnitTests;

[Trait("Category", "ReviewScheduling")]
public sealed class ReviewSchedulingTests
{
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
    private static readonly Guid ProfileId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void GeneralSuccessesFollowEveryDocumentedInterval()
    {
        ReviewItem item = Create(ReviewScheduleKind.General);
        Assert.Equal(new DateOnly(2026, 1, 2), item.DueOn);

        int[] expectedIntervals = [3, 7, 14, 30, 30];
        foreach (int expectedInterval in expectedIntervals)
        {
            DateTimeOffset answerAt = NoonUtc(item.DueOn);
            ReviewTransition transition = ReviewRules.Answer(
                item,
                new ReviewAnswer("réponse", true),
                ReviewPolicyCatalog.Version1,
                Paris,
                answerAt);

            Assert.Equal(ReviewOutcome.Succeeded, transition.Attempt.Outcome);
            Assert.Equal(expectedInterval, transition.Attempt.NextIntervalDays);
            Assert.Equal(item.DueOn.AddDays(expectedInterval), transition.Item.DueOn);
            item = transition.Item;
        }
    }

    [Fact]
    public void RecoverySkipsDayThreeAndStartsWithDayOneThenDaySeven()
    {
        ReviewItem item = Create(ReviewScheduleKind.Recovery);

        ReviewTransition first = ReviewRules.Answer(
            item,
            new ReviewAnswer("travail à blanc", true),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn));

        Assert.Equal(7, first.Attempt.NextIntervalDays);
        Assert.Equal(item.DueOn.AddDays(7), first.Item.DueOn);
    }

    [Fact]
    public void FailureAlwaysShortensTheNextIntervalToOneDay()
    {
        ReviewItem item = Create(ReviewScheduleKind.General);
        item = ReviewRules.Answer(
            item,
            new ReviewAnswer("réponse", true),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn)).Item;
        item = ReviewRules.Answer(
            item,
            new ReviewAnswer("réponse", true),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn)).Item;

        ReviewTransition failed = ReviewRules.Answer(
            item,
            new ReviewAnswer("je dois revoir", false),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn));

        Assert.Equal(ReviewOutcome.Failed, failed.Attempt.Outcome);
        Assert.Equal(1, failed.Attempt.NextIntervalDays);
        Assert.Equal(item.DueOn.AddDays(1), failed.Item.DueOn);
        Assert.False(failed.Attempt.IsMasteryEligible);
    }

    [Fact]
    public void TwoWeekDelayRestartsFromTheActualAnswerWithoutPenaltyOrBacklog()
    {
        ReviewItem item = Create(ReviewScheduleKind.General);
        DateOnly answeredOn = item.DueOn.AddDays(14);

        ReviewTransition transition = ReviewRules.Answer(
            item,
            new ReviewAnswer("réponse", true),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(answeredOn));

        Assert.Equal(3, transition.Attempt.NextIntervalDays);
        Assert.Equal(answeredOn.AddDays(3), transition.Item.DueOn);
        Assert.Equal(100m, transition.Attempt.Score);
    }

    [Fact]
    public void CalendarDaysRemainStableAcrossParisDayAndDstChange()
    {
        var source = Source(new DateTimeOffset(2026, 3, 28, 23, 30, 0, TimeSpan.Zero));
        ReviewItem item = ReviewRules.Create(
            ProfileId,
            source,
            MasteryDomain.CSharp,
            ReviewScheduleKind.General,
            SelfAssessedCard(),
            ReviewPolicyCatalog.Version1,
            Paris,
            new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 29), ReviewRules.LocalDate(source.OccurredAtUtc, Paris));
        Assert.Equal(new DateOnly(2026, 3, 30), item.DueOn);
    }

    [Fact]
    public void SameSourceIsDeterministicButARevisionCreatesAnotherItem()
    {
        ReviewItem first = Create(ReviewScheduleKind.General);
        ReviewItem replay = Create(ReviewScheduleKind.General);
        ReviewSource revisedSource = first.Source with { Revision = "content-v2" };
        ReviewItem revised = ReviewRules.Create(
            ProfileId,
            revisedSource,
            MasteryDomain.CSharp,
            ReviewScheduleKind.General,
            first.Card,
            ReviewPolicyCatalog.Version1,
            Paris,
            first.CreatedAtUtc);

        Assert.Equal(first.Id, replay.Id);
        Assert.NotEqual(first.Id, revised.Id);
        Assert.Equal(first.Source, replay.Source);
    }

    [Fact]
    public void OnlyServerCheckedMissedQuestionCanCreateMasteryEvidence()
    {
        DateTimeOffset occurred = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var choiceCard = new ReviewCard(
            "Quel choix est correct ?",
            "b",
            [new("a", "Premier"), new("b", "Second")],
            ReviewEvaluationMode.Choice,
            CanProduceMasteryEvidence: true);
        ReviewItem item = ReviewRules.Create(
            ProfileId,
            Source(occurred) with { Kind = ReviewSourceKind.MissedDiagnosticQuestion },
            MasteryDomain.CSharp,
            ReviewScheduleKind.Recovery,
            choiceCard,
            ReviewPolicyCatalog.Version1,
            Paris,
            occurred);

        ReviewTransition correct = ReviewRules.Answer(
            item,
            new ReviewAnswer("b", null),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn));
        ReviewTransition wrong = ReviewRules.Answer(
            item,
            new ReviewAnswer("a", null),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn));

        Assert.True(correct.Attempt.IsVerified);
        Assert.True(correct.Attempt.IsMasteryEligible);
        Assert.Equal(100m, correct.Attempt.Score);
        Assert.True(wrong.Attempt.IsMasteryEligible);
        Assert.Equal(0m, wrong.Attempt.Score);
    }

    [Fact]
    public void PersonalExactAnswerIsVerifiedButNeverChangesMastery()
    {
        DateTimeOffset occurred = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var card = new ReviewCard(
            "Ma question personnelle",
            "Une réponse nette",
            [],
            ReviewEvaluationMode.ExactText,
            CanProduceMasteryEvidence: false);
        ReviewItem item = ReviewRules.Create(
            ProfileId,
            Source(occurred) with { Kind = ReviewSourceKind.Personal },
            MasteryDomain.CSharp,
            ReviewScheduleKind.General,
            card,
            ReviewPolicyCatalog.Version1,
            Paris,
            occurred);

        ReviewTransition transition = ReviewRules.Answer(
            item,
            new ReviewAnswer("  une   RÉPONSE nette ", null),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn));

        Assert.Equal(ReviewOutcome.Succeeded, transition.Attempt.Outcome);
        Assert.True(transition.Attempt.IsVerified);
        Assert.False(transition.Attempt.IsMasteryEligible);
    }

    [Fact]
    public void PrematureAnswerAndFutureSourceFailClosed()
    {
        ReviewItem item = Create(ReviewScheduleKind.General);
        Assert.Throws<InvalidOperationException>(() => ReviewRules.Answer(
            item,
            new ReviewAnswer("réponse", true),
            ReviewPolicyCatalog.Version1,
            Paris,
            NoonUtc(item.DueOn.AddDays(-1))));

        DateTimeOffset now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        Assert.Throws<InvalidDataException>(() => ReviewRules.Create(
            ProfileId,
            Source(now.AddHours(1)),
            MasteryDomain.CSharp,
            ReviewScheduleKind.General,
            SelfAssessedCard(),
            ReviewPolicyCatalog.Version1,
            Paris,
            now));
    }

    [Fact]
    public void QueueProjectionCannotExposeExpectedAnswerAndControlCharactersAreRejected()
    {
        Assert.DoesNotContain(
            typeof(ReviewItemView).GetProperties(),
            property => property.Name.Contains("ExpectedAnswer", StringComparison.Ordinal));

        DateTimeOffset now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        ReviewCard hostile = SelfAssessedCard() with { Question = "Question\0interdite" };
        Assert.Throws<ArgumentException>(() => ReviewRules.Create(
            ProfileId,
            Source(now),
            MasteryDomain.CSharp,
            ReviewScheduleKind.General,
            hostile,
            ReviewPolicyCatalog.Version1,
            Paris,
            now));
    }

    private static ReviewItem Create(ReviewScheduleKind scheduleKind)
    {
        DateTimeOffset occurred = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        return ReviewRules.Create(
            ProfileId,
            Source(occurred),
            MasteryDomain.CSharp,
            scheduleKind,
            SelfAssessedCard(),
            ReviewPolicyCatalog.Version1,
            Paris,
            occurred);
    }

    private static ReviewSource Source(DateTimeOffset occurredAtUtc) => new(
        "source:stable",
        ReviewSourceKind.PracticeError,
        "csharp-item-001",
        1,
        "content-v1",
        occurredAtUtc);

    private static ReviewCard SelfAssessedCard() => new(
        "Explique et refais à blanc.",
        null,
        [],
        ReviewEvaluationMode.SelfAssessment,
        CanProduceMasteryEvidence: false);

    private static DateTimeOffset NoonUtc(DateOnly date) => new(
        date.Year,
        date.Month,
        date.Day,
        12,
        0,
        0,
        TimeSpan.Zero);
}
