using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForgeDotNet.Infrastructure.Persistence;

public sealed class ForgeDbContextFactory : IDesignTimeDbContextFactory<ForgeDbContext>
{
    public ForgeDbContext CreateDbContext(string[] args)
    {
        var dataDirectory = Environment.GetEnvironmentVariable("LocalData__DirectoryPath");
        var paths = LocalDataPaths.Create(new LocalDataOptions { DirectoryPath = dataDirectory });
        Directory.CreateDirectory(paths.DataDirectory);

        var options = new DbContextOptionsBuilder<ForgeDbContext>()
            .UseSqlite(paths.ConnectionString)
            .Options;

        return new ForgeDbContext(options);
    }
}
