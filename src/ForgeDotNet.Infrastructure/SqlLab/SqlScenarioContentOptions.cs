namespace ForgeDotNet.Infrastructure.SqlLab;

public sealed class SqlScenarioContentOptions
{
    public required string ContentRootPath { get; init; }

    /// <summary>Dossier des scénarios SQL publiés, typiquement <c>content/sql</c>.</summary>
    public required string ScenarioDirectoryPath { get; init; }
}
