namespace ForgeDotNet.Infrastructure.Curriculum;

public sealed class LessonContentOptions
{
    public required string ContentRootPath { get; init; }

    public required string CatalogDirectoryPath { get; init; }

    public string CurriculumId { get; init; } = "forge-reference";
}
