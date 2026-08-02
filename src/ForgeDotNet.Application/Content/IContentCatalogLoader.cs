using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Content;

public interface IContentCatalogLoader
{
    Task<ContentCatalogLoadResult> LoadAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);
}
