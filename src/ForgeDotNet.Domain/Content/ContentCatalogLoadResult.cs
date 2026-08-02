namespace ForgeDotNet.Domain.Content;

public sealed class ContentCatalogLoadResult
{
    private ContentCatalogLoadResult(
        ContentCatalog? catalog,
        IEnumerable<ContentValidationIssue> issues)
    {
        Catalog = catalog;
        Issues = issues
            .OrderBy(issue => issue.FilePath, StringComparer.Ordinal)
            .ThenBy(issue => issue.PropertyPath, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ToArray();
    }

    public bool Succeeded => Catalog is not null && Issues.Count == 0;

    public ContentCatalog? Catalog { get; }

    public IReadOnlyList<ContentValidationIssue> Issues { get; }

    public static ContentCatalogLoadResult Success(ContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return new ContentCatalogLoadResult(catalog, []);
    }

    public static ContentCatalogLoadResult Failure(IEnumerable<ContentValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ContentValidationIssue[] materialized = issues.ToArray();
        if (materialized.Length == 0)
        {
            throw new ArgumentException("Un échec de chargement doit contenir au moins une erreur.", nameof(issues));
        }

        return new ContentCatalogLoadResult(null, materialized);
    }
}
