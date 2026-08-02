using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

public sealed class ForgeWebApplicationFactory : WebApplicationFactory<Program>
{
    public ForgeWebApplicationFactory()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "ForgeDotNet.WebTests", Guid.NewGuid().ToString("N"));
    }

    public string DataDirectory { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("LocalData:DirectoryPath", DataDirectory);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["LocalData:DirectoryPath"] = DataDirectory,
            }));
        builder.ConfigureServices(services =>
            services.AddDataProtection().UseEphemeralDataProtectionProvider());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (disposing && Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }
}
