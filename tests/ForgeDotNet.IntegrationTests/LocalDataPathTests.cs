using ForgeDotNet.Infrastructure.Persistence;

namespace ForgeDotNet.IntegrationTests;

public sealed class LocalDataPathTests
{
    [Fact]
    public void DefaultPathUsesLocalApplicationDataAndStaysOutsideTheRepository()
    {
        var paths = LocalDataPaths.Create(new LocalDataOptions());

        Assert.StartsWith(
            Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
            paths.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(
            Path.Combine("Forge.NET", "data", LocalDataOptions.DefaultDatabaseFileName),
            paths.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RelativeDataDirectoryIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LocalDataPaths.Create(new LocalDataOptions { DirectoryPath = "relative-data" }));

        Assert.Contains("absolu", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DatabaseFileTraversalIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => LocalDataPaths.Create(new LocalDataOptions
        {
            DirectoryPath = Path.GetTempPath(),
            DatabaseFileName = Path.Combine("..", "outside.db"),
        }));
    }

    [Fact]
    public void ApplicationDirectoryIsRejectedAsDataDirectory()
    {
        var applicationDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ForgeDotNet.App"));
        var paths = LocalDataPaths.Create(new LocalDataOptions { DirectoryPath = applicationDirectory });

        Assert.Throws<InvalidOperationException>(() => paths.EnsureOutside(applicationDirectory));
    }
}
