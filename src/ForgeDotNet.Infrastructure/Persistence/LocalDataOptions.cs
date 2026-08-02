namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class LocalDataOptions
{
    public const string DefaultDatabaseFileName = "forge-dotnet.db";

    public string? DirectoryPath { get; init; }

    public string DatabaseFileName { get; init; } = DefaultDatabaseFileName;
}
