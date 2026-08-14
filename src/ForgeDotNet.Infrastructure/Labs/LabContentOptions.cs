namespace ForgeDotNet.Infrastructure.Labs;

public sealed class LabContentOptions
{
    public required string ContentRootPath { get; init; }

    /// <summary>Dossier des laboratoires publiés, typiquement <c>content/labs</c>.</summary>
    public required string LabDirectoryPath { get; init; }
}
