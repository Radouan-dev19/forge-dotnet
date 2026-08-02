using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Content;

public interface IContentValidationService
{
    Task<ContentValidationReport> ValidateAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);
}
