using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Content;

public sealed record ContentCatalogReloadResult(
    bool Succeeded,
    ContentCatalog Previous,
    ContentCatalog Current,
    IReadOnlyList<ContentValidationIssue> Issues)
{
    public bool PreviousSnapshotPreserved => !Succeeded && ReferenceEquals(Previous, Current);
}
