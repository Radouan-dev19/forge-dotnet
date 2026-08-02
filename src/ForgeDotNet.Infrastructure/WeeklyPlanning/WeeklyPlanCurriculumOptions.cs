namespace ForgeDotNet.Infrastructure.WeeklyPlanning;

public sealed class WeeklyPlanCurriculumOptions
{
    public const string DefaultFileName = "curriculum.json";

    public required string ContentRootPath { get; init; }

    public required string DirectoryPath { get; init; }

    public string FileName { get; init; } = DefaultFileName;
}
