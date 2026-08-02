using ForgeDotNet.Domain.Curriculum;

namespace ForgeDotNet.UnitTests;

public sealed class ReadingProgressTests
{
    [Fact]
    public void AVisitWithoutObservedActivityNeverCreatesProgress()
    {
        int percentage = ReadingProgress.CalculatePercentage(
            ["section:objectif", "quiz:check"],
            []);

        Assert.Equal(0, percentage);
    }

    [Fact]
    public void UnknownAndDuplicateActivitiesCannotInflateProgress()
    {
        int percentage = ReadingProgress.CalculatePercentage(
            ["section:objectif", "section:explication", "quiz:check"],
            ["section:objectif", "section:objectif", "forged:mastery"]);

        Assert.Equal(33, percentage);
    }
}
