using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.SqlLab;
using ForgeDotNet.Infrastructure.SqlLab;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ForgeDotNet.IntegrationTests;

[Collection("SqlEfContentSerial")]
public sealed class SqlLabSecurityTests
{
    [Fact]
    [Trait("Category", "SqlLabSecurity")]
    public async Task NormalSelectValidationRollbackAndResetAreReliable()
    {
        await using TestGateway environment = await TestGateway.CreateAsync();
        SqlLabSessionDescriptor session = await environment.Gateway.CreateSessionAsync();
        var service = new SqlLabService(environment.Gateway);

        SqlLabRunView initial = await service.ExecuteAsync(
            session.Id,
            SqlLabService.DefaultQuery,
            validateReference: true);
        Assert.Equal(SqlLabExecutionStatus.Succeeded, initial.Status);
        Assert.True(initial.Validation?.Passed);
        Assert.Equal(3, initial.Rows.Count);

        SqlLabExecutionResult mutation = await environment.Gateway.ExecuteAsync(
            session.Id,
            "UPDATE dbo.Orders SET Total = 999 WHERE OrderId = 1;",
            expectation: null);
        Assert.Equal(SqlLabExecutionStatus.Succeeded, mutation.Status);
        Assert.Contains(mutation.Effects, effect => effect.Name.StartsWith("OrdersCount", StringComparison.Ordinal) && effect.Value == "3");

        SqlLabRunView afterRollback = await service.ExecuteAsync(
            session.Id,
            SqlLabService.DefaultQuery,
            validateReference: true);
        Assert.True(afterRollback.Validation?.Passed);

        SqlLabSessionDescriptor reset = await environment.Gateway.ResetSessionAsync(session.Id);
        Assert.Equal(2, reset.Generation);
        SqlLabRunView afterReset = await service.ExecuteAsync(
            reset.Id,
            SqlLabService.DefaultQuery,
            validateReference: true);
        Assert.True(afterReset.Validation?.Passed);
    }

    [Fact]
    [Trait("Category", "SqlLabSecurity")]
    public async Task TimeoutResultQuotaAndCancellationStopHostileQueries()
    {
        await using TestGateway environment = await TestGateway.CreateAsync(
            queryTimeoutSeconds: 1,
            maximumRows: 5);
        SqlLabSessionDescriptor session = await environment.Gateway.CreateSessionAsync();

        SqlLabExecutionResult timeout = await environment.Gateway.ExecuteAsync(
            session.Id,
            "WAITFOR DELAY '00:00:10';",
            expectation: null);
        Assert.Equal(SqlLabExecutionStatus.TimedOut, timeout.Status);

        SqlLabExecutionResult massive = await environment.Gateway.ExecuteAsync(
            session.Id,
            "SELECT n FROM (VALUES (1), (2), (3), (4), (5), (6)) AS values_over_quota(n);",
            expectation: null);
        Assert.Equal(SqlLabExecutionStatus.ResultLimitExceeded, massive.Status);
        Assert.Null(massive.Result);

        SqlLabExecutionResult oversized = await environment.Gateway.ExecuteAsync(
            session.Id,
            "SELECT REPLICATE(CAST(N'x' AS nvarchar(max)), 70000) AS payload;",
            expectation: null);
        Assert.Equal(SqlLabExecutionStatus.ResultLimitExceeded, oversized.Status);
        Assert.Null(oversized.Result);

        await using TestGateway cancellationEnvironment = await TestGateway.CreateAsync(queryTimeoutSeconds: 10);
        SqlLabSessionDescriptor cancellationSession = await cancellationEnvironment.Gateway.CreateSessionAsync();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        SqlLabExecutionResult cancelled = await cancellationEnvironment.Gateway.ExecuteAsync(
            cancellationSession.Id,
            "WAITFOR DELAY '00:00:10';",
            expectation: null,
            cancellation.Token);
        Assert.Equal(SqlLabExecutionStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    [Trait("Category", "SqlLabSecurity")]
    public async Task TwoSessionsUseMinimalLoginsThatCannotReachOtherDatabaseServerOsOrProgression()
    {
        await using TestGateway environment = await TestGateway.CreateAsync();
        IReadOnlySet<string> databasesBefore = await environment.ListLabDatabasesAsync();
        SqlLabSessionDescriptor first = await environment.Gateway.CreateSessionAsync();
        _ = await environment.Gateway.CreateSessionAsync();
        string[] databases = (await environment.ListLabDatabasesAsync())
            .Except(databasesBefore, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, databases.Length);

        string ownDatabase = databases[0];
        string otherDatabase = databases[1];
        string token = ownDatabase["forge_lab_".Length..];
        string login = $"forge_user_{token}";
        string probePassword = "Probe!A9" + Guid.NewGuid().ToString("N");
        await environment.RotateLoginPasswordAsync(login, probePassword);
        await using SqlConnection connection = await TestGateway.OpenLabProbeAsync(ownDatabase, login, probePassword);

        await using (var roleCommand = new SqlCommand(
            "SELECT ISNULL(IS_SRVROLEMEMBER('sysadmin'), 0), ISNULL(IS_MEMBER('db_owner'), 0);",
            connection))
        await using (SqlDataReader reader = await roleCommand.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal(0, reader.GetInt32(0));
            Assert.Equal(0, reader.GetInt32(1));
        }

        Assert.Equal(3, await ScalarInt64Async(connection, "SELECT COUNT_BIG(*) FROM dbo.Orders;"));
        await AssertSqlDeniedAsync(connection, $"SELECT COUNT_BIG(*) FROM [{otherDatabase}].dbo.Orders;");
        await AssertSqlDeniedAsync(connection, "CREATE LOGIN forged_attack WITH PASSWORD = 'Attack!A912345';");
        await AssertSqlDeniedAsync(connection, "EXEC master.dbo.xp_cmdshell 'whoami';");
        await AssertSqlDeniedAsync(
            connection,
            "EXEC sp_execute_external_script @language=N'Python', @script=N'print(1)';");
        await AssertSqlDeniedAsync(
            connection,
            "BULK INSERT dbo.Orders FROM '/var/lib/forge-dotnet/forge-dotnet.db';");

        SqlLabExecutionResult adapterCrossDatabase = await environment.Gateway.ExecuteAsync(
            first.Id,
            $"SELECT * FROM [{otherDatabase}].dbo.Orders;",
            expectation: null);
        Assert.Equal(SqlLabExecutionStatus.Refused, adapterCrossDatabase.Status);
    }

    [Fact]
    [Trait("Category", "SqlLabSecurity")]
    public async Task PublicViewsAndStructuredLogsContainNoSecretServerLoginOrQueryBody()
    {
        var logger = new CapturingLogger<SqlServerLabGateway>();
        await using TestGateway environment = await TestGateway.CreateAsync(logger: logger);
        SqlLabSessionDescriptor session = await environment.Gateway.CreateSessionAsync();
        var service = new SqlLabService(environment.Gateway);

        SqlLabRunView failed = await service.ExecuteAsync(
            session.Id,
            "SELECT MissingColumn FROM dbo.Orders;",
            validateReference: false);
        string serialized = JsonSerializer.Serialize(new
        {
            Session = session,
            Result = failed,
        });
        string secret = await File.ReadAllTextAsync(environment.SecretPath);

        Assert.DoesNotContain(secret, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("forge_user_", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("MissingColumn", string.Join('\n', logger.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain(secret, string.Join('\n', logger.Messages), StringComparison.Ordinal);
        Assert.Equal(SqlLabExecutionStatus.Failed, failed.Status);
        Assert.NotEqual(Guid.Empty, failed.DiagnosticId);
    }

    private static async Task<long> ScalarInt64Async(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task AssertSqlDeniedAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 3 };
        SqlException exception = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.NotEqual(0, exception.Number);
    }

    private sealed class TestGateway : IAsyncDisposable
    {
        private readonly string _administratorPassword;

        private TestGateway(
            SqlServerLabGateway gateway,
            string secretPath,
            string administratorPassword)
        {
            Gateway = gateway;
            SecretPath = secretPath;
            _administratorPassword = administratorPassword;
        }

        public SqlServerLabGateway Gateway { get; }

        public string SecretPath { get; }

        public static async Task<TestGateway> CreateAsync(
            int queryTimeoutSeconds = 3,
            int maximumRows = 100,
            ILogger<SqlServerLabGateway>? logger = null)
        {
            string root = FindRepositoryRoot();
            string secretPath = Path.Combine(root, ".secrets", "sql-lab-sa-password.txt");
            if (!File.Exists(secretPath))
            {
                throw new InvalidOperationException(
                    "Secret SqlLab absent. Exécutez scripts/start-sql-lab.ps1 avant les tests Category=SqlLabSecurity.");
            }

            string password = (await File.ReadAllTextAsync(secretPath)).Trim();
            var options = new SqlLabOptions
            {
                Enabled = true,
                Server = "127.0.0.1",
                Port = 14333,
                AdministratorPasswordFile = secretPath,
                QueryTimeoutSeconds = queryTimeoutSeconds,
                MaximumRows = maximumRows,
                MaximumResultBytes = 65_536,
                MaximumSessions = 4,
                MaximumConcurrency = 2,
            };
            var gateway = new SqlServerLabGateway(
                options,
                TimeProvider.System,
                logger ?? new CapturingLogger<SqlServerLabGateway>());
            SqlLabAvailability availability = await gateway.GetAvailabilityAsync();
            if (!availability.Available)
            {
                await gateway.DisposeAsync();
                throw new InvalidOperationException(availability.Message);
            }

            return new TestGateway(gateway, secretPath, password);
        }

        public async Task<IReadOnlySet<string>> ListLabDatabasesAsync()
        {
            await using SqlConnection connection = await OpenAdministratorAsync();
            await using var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE name LIKE N'forge[_]lab[_]%';",
                connection);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            var names = new HashSet<string>(StringComparer.Ordinal);
            while (await reader.ReadAsync()) names.Add(reader.GetString(0));
            return names;
        }

        public async Task RotateLoginPasswordAsync(string login, string password)
        {
            Assert.Matches("^forge_user_[a-f0-9]{16}$", login);
            Assert.Matches("^[A-Za-z0-9!]+$", password);
            await using SqlConnection connection = await OpenAdministratorAsync();
            await using var command = new SqlCommand(
                $"ALTER LOGIN [{login}] WITH PASSWORD = N'{password}', CHECK_POLICY = ON;",
                connection);
            await command.ExecuteNonQueryAsync();
        }

        public static async Task<SqlConnection> OpenLabProbeAsync(string database, string login, string password)
        {
            var builder = CreateConnectionBuilder(database, login, password);
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async ValueTask DisposeAsync() => await Gateway.DisposeAsync();

        private async Task<SqlConnection> OpenAdministratorAsync()
        {
            var builder = CreateConnectionBuilder("master", "sa", _administratorPassword);
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static SqlConnectionStringBuilder CreateConnectionBuilder(
            string database,
            string user,
            string password) => new()
            {
                DataSource = "127.0.0.1,14333",
                InitialCatalog = database,
                UserID = user,
                Password = password,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 5,
                Pooling = false,
                PersistSecurityInfo = false,
            };

        private static string FindRepositoryRoot()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ForgeDotNet.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException("Racine Forge.NET introuvable.");
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Enqueue(formatter(state, exception));
    }
}
