using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.SqlContent;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ForgeDotNet.IntegrationTests;

[Collection("SqlEfContentSerial")]
public sealed class SqlEfContentTests
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IEnumerable<object[]> SqlScenarioIds => Directory
        .GetDirectories(FindSqlContentRoot())
        .Where(path => File.Exists(Path.Combine(path, "scenario.json")))
        .Where(path => string.Equals(ReadContract(path).Mode, "sql", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => new object[] { Path.GetFileName(path) });

    [Fact]
    [Trait("Category", "SqlEfContent")]
    public void MatrixContainsFortyCompleteUniqueAndSecretFreeScenarios()
    {
        string contentRoot = FindSqlContentRoot();
        string[] scenarioDirectories = Directory.GetDirectories(contentRoot)
            .Where(path => File.Exists(Path.Combine(path, "scenario.json")))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(40, scenarioDirectories.Length);

        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var families = new HashSet<string>(StringComparer.Ordinal);
        var weekCounts = new Dictionary<int, int>();
        int sqlCount = 0;
        int efCount = 0;
        foreach (string scenarioDirectory in scenarioDirectories)
        {
            using JsonDocument manifest = ReadJson(Path.Combine(scenarioDirectory, "scenario.json"));
            SqlContentContract contract = ReadContract(scenarioDirectory);
            string identifier = manifest.RootElement.GetProperty("id").GetString()!;
            Assert.True(identifiers.Add(identifier), $"Identifiant dupliqué : {identifier}");
            Assert.Equal(identifier, Path.GetFileName(scenarioDirectory));
            Assert.True(families.Add(contract.Family), $"Famille dupliquée : {contract.Family}");
            Assert.InRange(contract.Week, 8, 10);
            weekCounts[contract.Week] = weekCounts.GetValueOrDefault(contract.Week) + 1;
            Assert.False(string.IsNullOrWhiteSpace(contract.DatasetRevision));

            foreach (string fileName in new[] { "dataset.sql", "schema.sql", "reset.sql", "statement.md", "solution.md" })
            {
                var file = new FileInfo(Path.Combine(scenarioDirectory, fileName));
                Assert.True(file.Exists && file.Length > 0, $"Fichier requis absent ou vide : {identifier}/{fileName}");
            }

            string allPublicText = string.Join(
                '\n',
                Directory.GetFiles(scenarioDirectory, "*", SearchOption.AllDirectories)
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Select(File.ReadAllText));
            Assert.DoesNotContain("Password=", allPublicText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("User Id=", allPublicText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Server=", allPublicText, StringComparison.OrdinalIgnoreCase);
            string withoutDisposableSqlite = allPublicText.Replace(
                "Data Source=:memory:",
                string.Empty,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Data Source=", withoutDisposableSqlite, StringComparison.OrdinalIgnoreCase);

            if (contract.Mode == "sql") sqlCount++;
            else if (contract.Mode == "ef") efCount++;
            else Assert.Fail($"Mode inconnu : {contract.Mode}");
        }

        Assert.Equal(35, sqlCount);
        Assert.Equal(5, efCount);
        Assert.Equal(14, weekCounts[8]);
        Assert.Equal(13, weekCounts[9]);
        Assert.Equal(13, weekCounts[10]);
    }

    [Theory]
    [MemberData(nameof(SqlScenarioIds))]
    [Trait("Category", "SqlEfContent")]
    public async Task SqlSolutionEquivalentNegativeVariantPlanEffectsAndResetAreProven(string scenarioId)
    {
        string scenarioDirectory = Path.Combine(FindSqlContentRoot(), scenarioId);
        using JsonDocument manifest = ReadJson(Path.Combine(scenarioDirectory, "scenario.json"));
        SqlContentContract contract = ReadContract(scenarioDirectory);
        Assert.Equal("sql", contract.Mode);
        await using SqlContentDatabase database = await SqlContentDatabase.CreateAsync(
            scenarioId,
            manifest.RootElement.GetProperty("permissions").EnumerateArray().Select(value => value.GetString()!).ToArray(),
            allowMigrations: false);
        await database.ExecuteAdministratorAsync(File.ReadAllText(Path.Combine(scenarioDirectory, "dataset.sql")));

        SqlLabExpectedResult expectation = BuildExpectation(manifest.RootElement, contract);
        string solution = ExtractSqlSolution(File.ReadAllText(Path.Combine(scenarioDirectory, "solution.md")));
        await using (SqlConnection connection = await database.OpenLabAsync())
        await using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
        {
            SqlLabResultSet actual = await ExecuteResultSetAsync(
                connection,
                transaction,
                solution,
                manifest.RootElement.GetProperty("timeoutSeconds").GetInt32(),
                manifest.RootElement.GetProperty("maxRows").GetInt32());
            SqlLabValidationResult validation = SqlResultValidator.Validate(expectation, actual);
            Assert.True(validation.Passed, string.Join(Environment.NewLine, validation.Issues));

            await transaction.RollbackAsync();
        }

        await using (SqlConnection connection = await database.OpenLabAsync())
        await using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
        {
            SqlLabResultSet equivalent = await ExecuteResultSetAsync(
                connection,
                transaction,
                contract.EquivalentQuery!,
                manifest.RootElement.GetProperty("timeoutSeconds").GetInt32(),
                manifest.RootElement.GetProperty("maxRows").GetInt32());
            SqlLabValidationResult equivalentValidation = SqlResultValidator.Validate(expectation, equivalent);
            Assert.True(equivalentValidation.Passed, string.Join(Environment.NewLine, equivalentValidation.Issues));
            await transaction.RollbackAsync();
        }

        if (!string.IsNullOrWhiteSpace(contract.StabilityMutationSql))
        {
            await database.ExecuteAdministratorAsync(contract.StabilityMutationSql);
            await using SqlConnection stableConnection = await database.OpenLabAsync();
            await using SqlTransaction stableTransaction = (SqlTransaction)await stableConnection.BeginTransactionAsync();
            SqlLabResultSet stablePage = await ExecuteResultSetAsync(
                stableConnection,
                stableTransaction,
                solution,
                manifest.RootElement.GetProperty("timeoutSeconds").GetInt32(),
                manifest.RootElement.GetProperty("maxRows").GetInt32());
            Assert.True(SqlResultValidator.Validate(expectation, stablePage).Passed);
            await stableTransaction.RollbackAsync();
            await database.ExecuteAdministratorAsync(File.ReadAllText(Path.Combine(scenarioDirectory, "reset.sql")));
        }

        await using (SqlConnection connection = await database.OpenLabAsync())
        await using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
        {
            SqlLabResultSet negative = await ExecuteResultSetAsync(
                connection,
                transaction,
                contract.NegativeQuery!,
                manifest.RootElement.GetProperty("timeoutSeconds").GetInt32(),
                manifest.RootElement.GetProperty("maxRows").GetInt32());
            Assert.False(SqlResultValidator.Validate(expectation, negative).Passed);
            await transaction.RollbackAsync();
        }

        if (!string.IsNullOrWhiteSpace(contract.PlanIndex))
        {
            string plan = await database.ReadEstimatedPlanAsync(solution);
            Assert.Contains(contract.PlanIndex, plan, StringComparison.Ordinal);
            Assert.Contains("Index Seek", plan, StringComparison.OrdinalIgnoreCase);
        }

        await database.ExecuteAdministratorAsync(contract.DirtySql!);
        await database.ExecuteAdministratorAsync(File.ReadAllText(Path.Combine(scenarioDirectory, "reset.sql")));
        Assert.Equal(contract.ResetExpected, await database.ReadAdministratorScalarAsync(contract.ResetProbeSql!));
    }

    [Fact]
    [Trait("Category", "SqlEfContent")]
    public async Task MigrationSolutionIsIdempotentWhileStarterHasNoMigrationHistory()
    {
        string scenarioDirectory = ScenarioDirectory("ef-orders-migrations-001");
        await using SqlContentDatabase solutionDatabase = await SqlContentDatabase.CreateAsync(
            "ef-orders-migrations-001-solution",
            ["select", "insert", "update", "delete"],
            allowMigrations: true);
        string createTablePermission = await solutionDatabase.ReadLabScalarAsync(
            "SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE');");
        string permissionReport = await solutionDatabase.ReadLabScalarAsync(
            "SELECT STRING_AGG(state_desc + N':' + permission_name, N',') FROM sys.database_permissions WHERE grantee_principal_id = USER_ID();");
        Assert.True(createTablePermission == "1", $"CREATE TABLE={createTablePermission}; permissions={permissionReport}");
        Assert.Equal("0", await solutionDatabase.ReadLabScalarAsync("SELECT IS_SRVROLEMEMBER(N'sysadmin');"));
        Assert.Equal("0", await solutionDatabase.ReadLabScalarAsync("SELECT IS_ROLEMEMBER(N'db_owner');"));
        Assert.Equal("0", await solutionDatabase.ReadLabScalarAsync("SELECT HAS_PERMS_BY_NAME(NULL, NULL, N'ALTER ANY LOGIN');"));
        Assert.Equal("0", await solutionDatabase.ReadLabScalarAsync("SELECT HAS_PERMS_BY_NAME(NULL, NULL, N'ALTER ANY DATABASE');"));
        await using (MiniErpContext context = CreateContext(solutionDatabase.LabConnectionString))
        {
            await EfOrdersMigrationsSolution.ApplyAsync(context);
            await EfOrdersMigrationsSolution.ApplyAsync(context);
        }

        Assert.Equal("1", await solutionDatabase.ReadLabScalarAsync("SELECT COUNT_BIG(*) FROM dbo.__EFMigrationsHistory;"));
        Assert.Equal("2", await solutionDatabase.ReadLabScalarAsync("SELECT COUNT_BIG(*) FROM sys.tables WHERE name IN (N'Customers', N'Orders');"));
        Assert.Equal("1", await solutionDatabase.ReadLabScalarAsync("SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE name = N'FK_Orders_Customers_CustomerId';"));

        await using SqlContentDatabase starterDatabase = await SqlContentDatabase.CreateAsync(
            "ef-orders-migrations-001-starter",
            ["select", "insert", "update", "delete"],
            allowMigrations: true);
        await using (MiniErpContext context = CreateContext(starterDatabase.LabConnectionString))
        {
            Assert.True(await EfOrdersMigrationsStarter.ApplyAsync(context));
        }

        Assert.Equal("0", await starterDatabase.ReadLabScalarAsync("SELECT COUNT_BIG(*) FROM sys.tables WHERE name = N'__EFMigrationsHistory';"));
        await solutionDatabase.ExecuteAdministratorAsync(File.ReadAllText(Path.Combine(scenarioDirectory, "reset.sql")));
        Assert.Equal("0", await solutionDatabase.ReadAdministratorScalarAsync("SELECT COUNT_BIG(*) FROM sys.tables WHERE name IN (N'Customers', N'Orders', N'__EFMigrationsHistory');"));
    }

    [Fact]
    [Trait("Category", "SqlEfContent")]
    public async Task TrackingSolutionDetachesReadOnlyEntityAndStarterDoesNot()
    {
        await using SqlContentDatabase database = await CreateEfDatabaseAsync("ef-orders-tracking-001", ["select"]);
        await using (MiniErpContext solutionContext = CreateContext(database.LabConnectionString))
        {
            var solution = await EfOrdersTrackingSolution.ObserveAsync(solutionContext, 1);
            Assert.Equal((true, true, 1), solution);
        }

        await using (MiniErpContext starterContext = CreateContext(database.LabConnectionString))
        {
            var starter = await EfOrdersTrackingStarter.ObserveAsync(starterContext, 1);
            Assert.True(starter.SameTrackedInstance);
            Assert.False(starter.ReadOnlyEntityIsDetached);
        }

        await AssertEfResetAsync(database, "ef-orders-tracking-001");
    }

    [Fact]
    [Trait("Category", "SqlEfContent")]
    public async Task QueryableSolutionFiltersAndProjectsOnServerWhileStarterIgnoresThreshold()
    {
        await using SqlContentDatabase database = await CreateEfDatabaseAsync("ef-orders-queryable-001", ["select"]);
        await using (MiniErpContext context = CreateContext(database.LabConnectionString))
        {
            IQueryable<OrderSummary> solutionQuery = EfOrdersQueryableSolution.BuildQuery(context, 70m);
            string generatedSql = solutionQuery.ToQueryString();
            Assert.Contains("WHERE", generatedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("@minimumTotal", generatedSql, StringComparison.Ordinal);
            OrderSummary[] result = await solutionQuery.ToArrayAsync();
            Assert.Equal([1, 2], result.Select(item => item.OrderId));
            Assert.Equal(["Ada", "Ada"], result.Select(item => item.CustomerName));
        }

        await using (MiniErpContext context = CreateContext(database.LabConnectionString))
        {
            OrderSummary[] starter = await EfOrdersQueryableStarter.BuildQuery(context, 70m).ToArrayAsync();
            Assert.Equal(4, starter.Length);
        }

        await AssertEfResetAsync(database, "ef-orders-queryable-001");
    }

    [Fact]
    [Trait("Category", "SqlEfContent")]
    public async Task LoadingSolutionUsesOneCommandWhileStarterDemonstratesNPlusOne()
    {
        await using SqlContentDatabase database = await CreateEfDatabaseAsync("ef-orders-loading-001", ["select"]);
        var solutionCounter = new CountingCommandInterceptor();
        await using (MiniErpContext context = CreateContext(database.LabConnectionString, solutionCounter))
        {
            IReadOnlyList<CustomerOrderCount> result = await EfOrdersLoadingSolution.LoadAsync(context);
            Assert.Equal([2, 1, 1], result.Select(item => item.OrderCount));
        }

        Assert.Equal(1, solutionCounter.ReaderCommands);

        var starterCounter = new CountingCommandInterceptor();
        await using (MiniErpContext context = CreateContext(database.LabConnectionString, starterCounter))
        {
            IReadOnlyList<CustomerOrderCount> result = await EfOrdersLoadingStarter.LoadAsync(context);
            Assert.Equal([2, 1, 1], result.Select(item => item.OrderCount));
        }

        Assert.Equal(4, starterCounter.ReaderCommands);
        await AssertEfResetAsync(database, "ef-orders-loading-001");
    }

    [Fact]
    [Trait("Category", "SqlEfContent")]
    public async Task ConcurrencySolutionReportsConflictWhileStarterLeaksException()
    {
        await using SqlContentDatabase database = await CreateEfDatabaseAsync("ef-orders-concurrency-001", ["select", "update"]);
        await using (MiniErpContext first = CreateContext(database.LabConnectionString))
        await using (MiniErpContext second = CreateContext(database.LabConnectionString))
        {
            Assert.True(await EfOrdersConcurrencySolution.UpdateConcurrentlyAsync(first, second, 1));
        }

        await ResetEfDatasetAsync(database, "ef-orders-concurrency-001");
        await using (MiniErpContext first = CreateContext(database.LabConnectionString))
        await using (MiniErpContext second = CreateContext(database.LabConnectionString))
        {
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                EfOrdersConcurrencyStarter.UpdateConcurrentlyAsync(first, second, 1));
        }

        await AssertEfResetAsync(database, "ef-orders-concurrency-001");
    }

    private static async Task<SqlContentDatabase> CreateEfDatabaseAsync(string scenarioId, string[] permissions)
    {
        SqlContentDatabase database = await SqlContentDatabase.CreateAsync(scenarioId, permissions, allowMigrations: false);
        try
        {
            await database.ExecuteAdministratorAsync(File.ReadAllText(Path.Combine(ScenarioDirectory(scenarioId), "dataset.sql")));
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    private static MiniErpContext CreateContext(
        string connectionString,
        params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<MiniErpContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(InitialMiniErpMigration).Assembly.GetName().Name));
        if (interceptors.Length > 0) builder.AddInterceptors(interceptors);
        return new MiniErpContext(builder.Options);
    }

    private static async Task AssertEfResetAsync(SqlContentDatabase database, string scenarioId)
    {
        await database.ExecuteAdministratorAsync("UPDATE dbo.Orders SET Total = 0;");
        await ResetEfDatasetAsync(database, scenarioId);
        Assert.Equal("120.50", await database.ReadAdministratorScalarAsync("SELECT Total FROM dbo.Orders WHERE OrderId = 1;"));
        Assert.Equal("4", await database.ReadAdministratorScalarAsync("SELECT COUNT_BIG(*) FROM dbo.Orders;"));
    }

    private static Task ResetEfDatasetAsync(SqlContentDatabase database, string scenarioId) =>
        database.ExecuteAdministratorAsync(File.ReadAllText(Path.Combine(ScenarioDirectory(scenarioId), "reset.sql")));

    private static SqlLabExpectedResult BuildExpectation(JsonElement manifest, SqlContentContract contract)
    {
        string[] columns = manifest.GetProperty("expectedResult").GetProperty("columns")
            .EnumerateArray().Select(value => value.GetString()!).ToArray();
        IReadOnlyList<IReadOnlyList<SqlLabCell>> rows = contract.ExpectedRows!
            .Select(row => (IReadOnlyList<SqlLabCell>)row.Select(value => new SqlLabCell(value)).ToArray())
            .ToArray();
        return new SqlLabExpectedResult(
            columns,
            rows,
            manifest.GetProperty("expectedResult").GetProperty("ordered").GetBoolean(),
            manifest.GetProperty("expectedResult").GetProperty("numericTolerance").GetDecimal());
    }

    private static async Task<SqlLabResultSet> ExecuteResultSetAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string query,
        int timeoutSeconds,
        int maximumRows)
    {
        await using var command = new SqlCommand(query, connection, transaction) { CommandTimeout = timeoutSeconds };
        await using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult);
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(index => new SqlLabColumn(reader.GetName(index), reader.GetDataTypeName(index), IsNullable: true))
            .ToArray();
        var rows = new List<IReadOnlyList<SqlLabCell>>();
        while (await reader.ReadAsync())
        {
            Assert.True(rows.Count < maximumRows, $"Résultat supérieur au quota de {maximumRows} lignes.");
            var cells = new SqlLabCell[reader.FieldCount];
            for (int index = 0; index < reader.FieldCount; index++)
            {
                if (await reader.IsDBNullAsync(index)) cells[index] = new SqlLabCell(null, IsNull: true);
                else cells[index] = new SqlLabCell(NormalizeValue(reader.GetValue(index)));
            }

            rows.Add(cells);
        }

        return new SqlLabResultSet(columns, rows);
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string sql)
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 10 };
        _ = await command.ExecuteNonQueryAsync();
    }

    private static string NormalizeValue(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        decimal number => number.ToString("0.############################", CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        float number => number.ToString("R", CultureInfo.InvariantCulture),
        byte[] bytes => Convert.ToHexString(bytes),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static string ExtractSqlSolution(string markdown)
    {
        Match match = Regex.Match(markdown, "```sql\\s*(?<sql>.*?)```", RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(match.Success, "Bloc SQL de solution absent.");
        return match.Groups["sql"].Value.Trim();
    }

    private static SqlContentContract ReadContract(string scenarioDirectory) =>
        JsonSerializer.Deserialize<SqlContentContract>(
            File.ReadAllText(Path.Combine(scenarioDirectory, "tests", "contract.json")),
            ContractJsonOptions)
        ?? throw new InvalidDataException("Contrat de test SQL/EF invalide.");

    private static JsonDocument ReadJson(string path) => JsonDocument.Parse(File.ReadAllBytes(path));

    private static string ScenarioDirectory(string scenarioId) => Path.Combine(FindSqlContentRoot(), scenarioId);

    private static string FindSqlContentRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current is not null)
        {
            string candidate = Path.Combine(current, "content", "sql");
            if (Directory.Exists(candidate)) return candidate;
            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Racine content/sql introuvable.");
    }

    private sealed record SqlContentContract(
        int Week,
        string Family,
        string Mode,
        string DatasetRevision,
        string[][]? ExpectedRows,
        string? EquivalentQuery,
        string? NegativeQuery,
        string? DirtySql,
        string? ResetProbeSql,
        string? ResetExpected,
        string? PlanIndex,
        string? StabilityMutationSql);

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        public int ReaderCommands { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ReaderCommands++;
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommands++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SqlContentDatabase : IAsyncDisposable
    {
        private readonly string _administratorConnectionString;
        private readonly string _databaseName;
        private readonly string _loginName;

        private SqlContentDatabase(
            string administratorConnectionString,
            string databaseName,
            string loginName,
            string labConnectionString)
        {
            _administratorConnectionString = administratorConnectionString;
            _databaseName = databaseName;
            _loginName = loginName;
            LabConnectionString = labConnectionString;
        }

        public string LabConnectionString { get; }

        public static async Task<SqlContentDatabase> CreateAsync(
            string scenarioId,
            IReadOnlyCollection<string> permissions,
            bool allowMigrations)
        {
            string repositoryRoot = Directory.GetParent(FindSqlContentRoot())!.Parent!.FullName;
            string secretPath = Path.Combine(repositoryRoot, ".secrets", "sql-lab-sa-password.txt");
            if (!File.Exists(secretPath))
            {
                throw new InvalidOperationException("Secret SqlLab absent. Démarrez scripts/start-sql-lab.ps1.");
            }

            string administratorPassword = (await File.ReadAllTextAsync(secretPath)).Trim();
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
            string databaseName = $"forge_content_{token}";
            string loginName = $"forge_content_user_{token}";
            string loginPassword = $"F!{Convert.ToHexString(RandomNumberGenerator.GetBytes(18))}a9";
            string administratorConnectionString = BuildConnectionString("master", "sa", administratorPassword);
            string labConnectionString = BuildConnectionString(databaseName, loginName, loginPassword);
            var environment = new SqlContentDatabase(
                administratorConnectionString,
                databaseName,
                loginName,
                labConnectionString);
            try
            {
                await environment.ExecuteMasterAsync($"""
                    CREATE DATABASE [{databaseName}];
                    ALTER DATABASE [{databaseName}] SET TRUSTWORTHY OFF;
                    ALTER DATABASE [{databaseName}] SET DB_CHAINING OFF;
                    CREATE LOGIN [{loginName}] WITH PASSWORD = N'{loginPassword}', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [{databaseName}];
                    DENY VIEW SERVER STATE TO [{loginName}];
                    DENY ALTER ANY LOGIN TO [{loginName}];
                    {(allowMigrations ? string.Empty : $"DENY ALTER ANY DATABASE TO [{loginName}];")}
                    """);
                var grants = new List<string>();
                foreach (string permission in permissions)
                {
                    grants.Add(permission switch
                    {
                        "select" => $"GRANT SELECT ON SCHEMA::dbo TO [{loginName}];",
                        "insert" => $"GRANT INSERT ON SCHEMA::dbo TO [{loginName}];",
                        "update" => $"GRANT UPDATE ON SCHEMA::dbo TO [{loginName}];",
                        "delete" => $"GRANT DELETE ON SCHEMA::dbo TO [{loginName}];",
                        "execute" => $"GRANT EXECUTE ON SCHEMA::dbo TO [{loginName}];",
                        _ => throw new InvalidDataException($"Permission de contenu inconnue pour {scenarioId}."),
                    });
                }

                if (allowMigrations)
                {
                    grants.Add($"GRANT CREATE TABLE TO [{loginName}];");
                    grants.Add($"GRANT ALTER ON SCHEMA::dbo TO [{loginName}];");
                    grants.Add($"GRANT REFERENCES TO [{loginName}];");
                    grants.Add($"GRANT VIEW DEFINITION TO [{loginName}];");
                }

                await environment.ExecuteAdministratorAsync($"""
                    CREATE USER [{loginName}] FOR LOGIN [{loginName}] WITH DEFAULT_SCHEMA = dbo;
                    {string.Join(Environment.NewLine, grants)}
                    DENY TAKE OWNERSHIP TO [{loginName}];
                    """);
                return environment;
            }
            catch (Exception creationException)
            {
                try
                {
                    await environment.DisposeAsync();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(
                        $"La création de l'environnement SQL de {scenarioId} et son nettoyage ont tous deux échoué.",
                        creationException,
                        cleanupException);
                }

                throw;
            }
        }

        public async Task<SqlConnection> OpenLabAsync()
        {
            var connection = new SqlConnection(LabConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public Task ExecuteAdministratorAsync(string sql) => ExecuteAsync(DatabaseAdministratorConnectionString(), sql);

        public Task<string> ReadAdministratorScalarAsync(string sql) => ReadScalarAsync(DatabaseAdministratorConnectionString(), sql);

        public Task<string> ReadLabScalarAsync(string sql) => ReadScalarAsync(LabConnectionString, sql);

        public async Task<string> ReadEstimatedPlanAsync(string query)
        {
            await using var connection = new SqlConnection(DatabaseAdministratorConnectionString());
            await connection.OpenAsync();
            await using (var enable = new SqlCommand("SET SHOWPLAN_XML ON;", connection)) await enable.ExecuteNonQueryAsync();
            try
            {
                await using var command = new SqlCommand(query, connection) { CommandTimeout = 30 };
                object? value = await command.ExecuteScalarAsync();
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            finally
            {
                await using var disable = new SqlCommand("SET SHOWPLAN_XML OFF;", connection);
                await disable.ExecuteNonQueryAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await ExecuteMasterAsync($"""
                IF DB_ID(N'{_databaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_databaseName}];
                END;
                IF SUSER_ID(N'{_loginName}') IS NOT NULL DROP LOGIN [{_loginName}];
                """);
        }

        private string DatabaseAdministratorConnectionString()
        {
            var builder = new SqlConnectionStringBuilder(_administratorConnectionString) { InitialCatalog = _databaseName };
            return builder.ConnectionString;
        }

        private Task ExecuteMasterAsync(string sql) => ExecuteAsync(_administratorConnectionString, sql);

        private static async Task ExecuteAsync(string connectionString, string sql)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
            _ = await command.ExecuteNonQueryAsync();
        }

        private static async Task<string> ReadScalarAsync(string connectionString, string sql)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            object? value = await command.ExecuteScalarAsync();
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string BuildConnectionString(string database, string user, string password) =>
            new SqlConnectionStringBuilder
            {
                DataSource = "127.0.0.1,14333",
                InitialCatalog = database,
                UserID = user,
                Password = password,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 15,
                Pooling = false,
                PersistSecurityInfo = false,
                ApplicationName = "Forge.NET SqlEfContentTests",
            }.ConnectionString;
    }
}

[CollectionDefinition("SqlEfContentSerial", DisableParallelization = true)]
public sealed class SqlEfContentSerialDefinition;
