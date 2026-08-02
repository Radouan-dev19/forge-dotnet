namespace ForgeDotNet.Infrastructure.Content;

public sealed class ContentValidationOptions
{
    public const long DefaultMaximumFileSizeBytes = 256 * 1024;
    public const int DefaultMaximumFiles = 10_000;

    public required string ContentRootPath { get; init; }

    public string? SchemaRootPath { get; init; }

    public long MaximumFileSizeBytes { get; init; } = DefaultMaximumFileSizeBytes;

    public int MaximumFiles { get; init; } = DefaultMaximumFiles;
}
