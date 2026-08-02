using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.UnitTests;

public sealed class ContentValidationReportTests
{
    [Fact]
    public void InvalidReportAcceptsNoDocumentAndSortsIssuesDeterministically()
    {
        var report = new ContentValidationReport(
            filesExamined: 3,
            documentsExamined: 2,
            [
                new ContentValidationIssue("required", "z.json", "$.title", "Titre absent."),
                new ContentValidationIssue("type", "a.json", "$.version", "Type invalide."),
            ]);

        Assert.False(report.IsValid);
        Assert.Equal(0, report.AcceptedDocuments);
        Assert.Equal(["a.json", "z.json"], report.Issues.Select(issue => issue.FilePath));
    }

    [Fact]
    public void ValidReportAcceptsEveryExaminedDocument()
    {
        var report = new ContentValidationReport(8, 8, []);

        Assert.True(report.IsValid);
        Assert.Equal(8, report.AcceptedDocuments);
    }
}
