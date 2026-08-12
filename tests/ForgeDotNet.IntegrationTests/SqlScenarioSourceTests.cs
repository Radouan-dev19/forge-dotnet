using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Infrastructure.Content;
using ForgeDotNet.Infrastructure.SqlLab;

namespace ForgeDotNet.IntegrationTests;

/// <summary>
/// P1-03 : les scénarios SQL livrés n'étaient atteignables par aucun parcours utilisateur.
/// </summary>
/// <remarks>
/// Ces tests ne demandent ni Docker ni SQL Server : ils portent sur le chargement du contenu et sur
/// la frontière public/privé, indépendamment de l'exécution des requêtes.
/// </remarks>
public sealed class SqlScenarioSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ContentRoot = Path.Combine(RepositoryRoot, "content");
    private static readonly string ScenarioRoot = Path.Combine(ContentRoot, "sql");

    [Fact]
    public async Task EveryPublishedSqlScenarioLoadsWithItsOwnIdentityAndExpectedRows()
    {
        using SqlScenarioCatalog catalog = await CreateCatalogAsync();
        FileSystemSqlScenarioSource source = CreateSource(catalog);

        IReadOnlyList<SqlScenario> scenarios = await source.ListAsync();

        // Les scénarios EF Core s'exécutent dans le runner isolé et ne sont pas offerts ici.
        Assert.Equal(35, scenarios.Count);
        Assert.All(scenarios, scenario =>
        {
            Assert.False(string.IsNullOrWhiteSpace(scenario.Statement));
            Assert.False(string.IsNullOrWhiteSpace(scenario.VisibleSchema));
            Assert.Contains("CREATE TABLE", scenario.SchemaAndDatasetSql, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(scenario.Expectation.Columns);
            Assert.NotEmpty(scenario.Expectation.Rows);
            Assert.All(
                scenario.Expectation.Rows,
                row => Assert.Equal(scenario.Expectation.Columns.Count, row.Count));
            Assert.Equal(64, scenario.ContentRevision.Length);
        });

        // Chaque scénario porte une identité distincte : une preuve ne peut plus être attribuée
        // à l'identité fictive unique que le service utilisait auparavant.
        Assert.Equal(scenarios.Count, scenarios.Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(scenarios, scenario => scenario.Id == "sql-lab-reference-001");
    }

    [Fact]
    public async Task ScenarioLimitsNeverExceedTheConfiguredLaboratoryBounds()
    {
        using SqlScenarioCatalog catalog = await CreateCatalogAsync();
        SqlLabOptions labOptions = CreateLabOptions();
        FileSystemSqlScenarioSource source = CreateSource(catalog, labOptions);

        IReadOnlyList<SqlScenario> scenarios = await source.ListAsync();

        Assert.All(scenarios, scenario =>
        {
            Assert.True(scenario.Limits.TimeoutSeconds <= labOptions.QueryTimeoutSeconds);
            Assert.True(scenario.Limits.MaximumRows <= labOptions.MaximumRows);
        });
    }

    [Fact]
    public async Task PublicScenarioViewCarriesNeitherExpectedRowsNorSolution()
    {
        using SqlScenarioCatalog catalog = await CreateCatalogAsync();
        var service = new SqlLabService(new UnavailableGateway(), CreateSource(catalog));

        SqlScenarioView view = Assert.IsType<SqlScenarioView>(
            await service.GetScenarioAsync("sql-monthly-cte-001"));

        string serialized = System.Text.Json.JsonSerializer.Serialize(view);
        SqlScenario scenario = Assert.IsType<SqlScenario>(
            await CreateSource(catalog).GetAsync("sql-monthly-cte-001"));

        // Les noms de colonnes font partie de la consigne ; les valeurs attendues, non.
        Assert.Equal(scenario.Expectation.Columns, view.ExpectedColumns);
        foreach (IReadOnlyList<SqlLabCell> row in scenario.Expectation.Rows)
        {
            foreach (SqlLabCell cell in row.Where(cell => !cell.IsNull && cell.Value!.Length > 3))
            {
                Assert.DoesNotContain(cell.Value!, serialized, StringComparison.Ordinal);
            }
        }

        Assert.DoesNotContain("WITH Monthly", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solution", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownScenarioIsRefusedInsteadOfSilentlyFallingBackToTheSandbox()
    {
        using SqlScenarioCatalog catalog = await CreateCatalogAsync();
        var service = new SqlLabService(new UnavailableGateway(), CreateSource(catalog));

        Assert.Null(await service.GetScenarioAsync("sql-does-not-exist-001"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ExecuteAsync(Guid.NewGuid(), "SELECT 1;", validateReference: true, "sql-does-not-exist-001"));
    }

    [Fact]
    public async Task PublishedScenarioListIsOfferedByTheHomeView()
    {
        using SqlScenarioCatalog catalog = await CreateCatalogAsync();
        var service = new SqlLabService(new UnavailableGateway(), CreateSource(catalog));

        SqlLabHomeView home = await service.GetHomeAsync();

        Assert.False(home.Available);
        Assert.Equal(35, home.Scenarios.Count);
        Assert.All(home.Scenarios, summary => Assert.False(string.IsNullOrWhiteSpace(summary.Title)));
    }

    private static async Task<SqlScenarioCatalog> CreateCatalogAsync()
    {
        var catalog = new SqlScenarioCatalog(
            new ContentValidationOptions { ContentRootPath = ContentRoot },
            ScenarioRoot);
        await catalog.LoadAsync();
        return catalog;
    }

    private static FileSystemSqlScenarioSource CreateSource(
        SqlScenarioCatalog catalog,
        SqlLabOptions? labOptions = null) => new(
            catalog,
            new SqlScenarioContentOptions
            {
                ContentRootPath = ContentRoot,
                ScenarioDirectoryPath = ScenarioRoot,
            },
            labOptions ?? CreateLabOptions());

    /// <summary>Bornes par défaut : le moteur n'est pas démarré, seules les limites comptent ici.</summary>
    private static SqlLabOptions CreateLabOptions() => new();

    /// <summary>Passerelle inerte : ces tests ne démarrent aucun moteur SQL.</summary>
    private sealed class UnavailableGateway : ISqlLabGateway
    {
        public Task<SqlLabAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqlLabAvailability(false, "SqlLab est désactivé."));

        public Task<SqlLabSessionDescriptor> CreateSessionAsync(
            SqlLabProvisioning? provisioning = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<SqlLabSessionDescriptor> ResetSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SqlLabExecutionResult> ExecuteAsync(
            Guid sessionId,
            string query,
            SqlLabExpectedResult? expectation,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Racine du dépôt de test introuvable.");
    }
}
