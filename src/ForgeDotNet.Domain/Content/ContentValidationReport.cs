namespace ForgeDotNet.Domain.Content;

public sealed class ContentValidationReport
{
    public ContentValidationReport(
        int filesExamined,
        int documentsExamined,
        IEnumerable<ContentValidationIssue> issues)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(filesExamined);
        ArgumentOutOfRangeException.ThrowIfNegative(documentsExamined);
        ArgumentNullException.ThrowIfNull(issues);

        FilesExamined = filesExamined;
        DocumentsExamined = documentsExamined;
        Issues = issues
            .OrderBy(issue => issue.FilePath, StringComparer.Ordinal)
            .ThenBy(issue => issue.PropertyPath, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ToArray();
    }

    public int FilesExamined { get; }

    public int DocumentsExamined { get; }

    public int AcceptedDocuments => IsValid ? DocumentsExamined : 0;

    public IReadOnlyList<ContentValidationIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;
}
