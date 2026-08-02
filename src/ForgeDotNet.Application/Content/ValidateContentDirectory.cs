using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Content;

public sealed class ValidateContentDirectory(IContentValidationService validationService)
{
    public Task<ContentValidationReport> ExecuteAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        return validationService.ValidateAsync(directoryPath, cancellationToken);
    }
}
