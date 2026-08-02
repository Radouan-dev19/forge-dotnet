using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.UnitTests;

public sealed class DiagnosticTimelineTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ServerDeadlineExpiresSectionAndRefusesFurtherAnswers()
    {
        DiagnosticTimeline timeline = DiagnosticTimelineRules.CreateStarted(
            sectionCount: 3,
            Start,
            TimeSpan.FromMinutes(1));

        DiagnosticTimeline expired = DiagnosticTimelineRules.Refresh(timeline, Start.AddMinutes(1));

        Assert.Equal(DiagnosticSectionStatus.Expired, expired.SectionStatuses[0]);
        Assert.Equal(1, expired.CurrentSectionIndex);
        Assert.Null(expired.SectionDeadlineUtc);
        Assert.False(DiagnosticTimelineRules.CanAnswer(expired, sectionIndex: 0, Start.AddMinutes(1)));
    }

    [Fact]
    public void CompletingSameSectionTwiceIsIdempotentAndDoesNotStartNextTimer()
    {
        DiagnosticTimeline timeline = DiagnosticTimelineRules.CreateStarted(
            sectionCount: 3,
            Start,
            TimeSpan.FromMinutes(1));

        DiagnosticTimeline completed = DiagnosticTimelineRules.CompleteCurrent(timeline, 0, Start.AddSeconds(10));
        DiagnosticTimeline duplicate = DiagnosticTimelineRules.CompleteCurrent(completed, 0, Start.AddSeconds(11));

        Assert.Equal(completed, duplicate);
        Assert.Equal(DiagnosticSectionStatus.Pending, duplicate.SectionStatuses[1]);
        Assert.Null(duplicate.SectionDeadlineUtc);
    }

    [Fact]
    public void FinishRequiresEverySectionToBeCompletedOrExpired()
    {
        DiagnosticTimeline timeline = DiagnosticTimelineRules.CreateStarted(
            sectionCount: 2,
            Start,
            TimeSpan.FromMinutes(1));

        Assert.Throws<InvalidOperationException>(() => DiagnosticTimelineRules.Finish(timeline, Start));

        timeline = DiagnosticTimelineRules.CompleteCurrent(timeline, 0, Start.AddSeconds(1));
        timeline = DiagnosticTimelineRules.StartCurrent(timeline, Start.AddSeconds(2), TimeSpan.FromMinutes(1));
        timeline = DiagnosticTimelineRules.Refresh(timeline, Start.AddMinutes(2));
        timeline = DiagnosticTimelineRules.Finish(timeline, Start.AddMinutes(2));

        Assert.Equal(DiagnosticSessionStatus.Completed, timeline.SessionStatus);
        Assert.Equal(DiagnosticSectionStatus.Expired, timeline.SectionStatuses[1]);
    }
}
