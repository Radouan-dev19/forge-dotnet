using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.IntegrationTests;

internal sealed class PersistenceTestEnvironment : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly bool _deleteOnDispose;

    private PersistenceTestEnvironment(string dataDirectory, ServiceProvider services, bool deleteOnDispose)
    {
        DataDirectory = dataDirectory;
        _services = services;
        _deleteOnDispose = deleteOnDispose;
    }

    public string DataDirectory { get; }

    public string DatabasePath => GetRequiredService<ILocalDataLocation>().DatabasePath;

    public T GetRequiredService<T>() where T : notnull => _services.GetRequiredService<T>();

    public static async Task<PersistenceTestEnvironment> CreateAsync(
        string? existingDirectory = null,
        bool deleteOnDispose = true,
        TimeProvider? timeProvider = null)
    {
        var dataDirectory = existingDirectory
            ?? Path.Combine(Path.GetTempPath(), "ForgeDotNet.Tests", Guid.NewGuid().ToString("N"));
        var paths = LocalDataPaths.Create(new LocalDataOptions { DirectoryPath = dataDirectory });
        var services = new ServiceCollection().AddLogging();
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        ServiceProvider provider = services
            .AddForgeLocalPersistence(paths)
            .BuildServiceProvider(validateScopes: true);
        var environment = new PersistenceTestEnvironment(dataDirectory, provider, deleteOnDispose);
        await environment.GetRequiredService<LocalDatabaseInitializer>().MigrateAsync();
        return environment;
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();

        if (_deleteOnDispose && Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }
}
