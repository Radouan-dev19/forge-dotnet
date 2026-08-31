using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

public class ForgeWebApplicationFactory : WebApplicationFactory<Program>
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
        builder.UseSetting("SqlLab:Enabled", "false");
        // Les tests Web ne peuvent pas dérouler le formulaire interactif de déverrouillage : la
        // garde des dossiers de préparation est levée ici, et testée par sa propre factory.
        builder.UseSetting("Prep:RequirePassword", "false");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["LocalData:DirectoryPath"] = DataDirectory,
                // Le profil Development active SqlLab pour le poste du développeur ; les tests Web
                // n'ont pas de SQL Server et vérifient le mode désactivé, honnête et déterministe.
                ["SqlLab:Enabled"] = "false",
                ["Prep:RequirePassword"] = "false",
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
