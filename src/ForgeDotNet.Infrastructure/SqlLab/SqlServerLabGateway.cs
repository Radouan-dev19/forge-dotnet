using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.SqlLab;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ForgeDotNet.Infrastructure.SqlLab;

public sealed class SqlServerLabGateway : ISqlLabGateway, IAsyncDisposable
{
    private static readonly Action<ILogger, string, Exception?> LogCleanupFailure = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(6101, "SqlLabCleanupFailure"),
        "Nettoyage SqlLab incomplet pendant {Phase}.");

    private static readonly Action<ILogger, string, Guid, int, Exception?> LogSqlFailureMessage =
        LoggerMessage.Define<string, Guid, int>(
            LogLevel.Warning,
            new EventId(6102, "SqlLabExecutionFailure"),
            "SqlLab {Category}; diagnostic={DiagnosticId}; sqlNumber={SqlNumber}.");

    private readonly SqlLabOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SqlServerLabGateway> _logger;
    private readonly ConcurrentDictionary<Guid, SessionLease> _sessions = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim _executionGate;

    public SqlServerLabGateway(
        SqlLabOptions options,
        TimeProvider timeProvider,
        ILogger<SqlServerLabGateway> logger)
    {
        options.Validate();
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
        _executionGate = new SemaphoreSlim(options.MaximumConcurrency, options.MaximumConcurrency);
    }

    public async Task<SqlLabAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new SqlLabAvailability(false, "SqlLab est désactivé. Démarrez le profil Compose sql-lab.");
        }

        try
        {
            await using SqlConnection connection = await OpenAdministratorConnectionAsync("master", cancellationToken);
            await using var command = new SqlCommand("SELECT CAST(1 AS int);", connection)
            {
                CommandTimeout = _options.ConnectTimeoutSeconds,
            };
            _ = await command.ExecuteScalarAsync(cancellationToken);
            return new SqlLabAvailability(true, "SQL Server de laboratoire est disponible.");
        }
        catch (Exception exception) when (exception is SqlException or IOException or UnauthorizedAccessException)
        {
            return new SqlLabAvailability(false, "SQL Server de laboratoire est indisponible. Vérifiez le service Compose et son secret local.");
        }
    }

    public async Task<SqlLabSessionDescriptor> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_sessions.Count >= _options.MaximumSessions)
            {
                throw new InvalidOperationException("Le nombre maximal de sessions SQL jetables est atteint.");
            }

            SessionLease lease = await ProvisionLeaseAsync(generation: 1, cancellationToken);
            if (!_sessions.TryAdd(lease.Id, lease))
            {
                await CleanupLeaseAsync(lease, CancellationToken.None);
                throw new InvalidOperationException("La session SQL n’a pas pu être enregistrée.");
            }

            return Describe(lease);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<SqlLabSessionDescriptor> ResetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        if (!_sessions.TryGetValue(sessionId, out SessionLease? current))
        {
            throw new KeyNotFoundException("La session SQL est inconnue ou déjà détruite.");
        }

        await _lifecycleGate.WaitAsync(cancellationToken);
        await current.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!_sessions.TryGetValue(sessionId, out SessionLease? latest) || !ReferenceEquals(current, latest))
            {
                throw new KeyNotFoundException("La session SQL est inconnue ou déjà détruite.");
            }

            SessionLease replacement = await ProvisionLeaseAsync(current.Generation + 1, cancellationToken, sessionId);
            try
            {
                await CleanupLeaseAsync(current, cancellationToken);
            }
            catch
            {
                await CleanupLeaseAsync(replacement, CancellationToken.None);
                throw;
            }

            _sessions[sessionId] = replacement;
            return Describe(replacement);
        }
        finally
        {
            current.Gate.Release();
            _lifecycleGate.Release();
        }
    }

    public async Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (!_sessions.TryRemove(sessionId, out SessionLease? lease))
            {
                return;
            }

            await lease.Gate.WaitAsync(cancellationToken);
            bool cleaned = false;
            try
            {
                await CleanupLeaseAsync(lease, cancellationToken);
                cleaned = true;
            }
            finally
            {
                lease.Gate.Release();
                if (cleaned)
                {
                    lease.Gate.Dispose();
                }
                else
                {
                    _sessions.TryAdd(sessionId, lease);
                }
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<SqlLabExecutionResult> ExecuteAsync(
        Guid sessionId,
        string query,
        SqlLabExpectedResult? expectation,
        CancellationToken cancellationToken = default)
    {
        Guid diagnosticId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        if (!_options.Enabled)
        {
            return Result(SqlLabExecutionStatus.Unavailable, "SqlLab est désactivé.", diagnosticId, stopwatch);
        }

        IReadOnlyList<string> guardIssues = SqlStatementGuard.Validate(query, _options.MaximumQueryCharacters);
        if (guardIssues.Count > 0)
        {
            return Result(SqlLabExecutionStatus.Refused, string.Join(' ', guardIssues), diagnosticId, stopwatch);
        }

        if (!_sessions.TryGetValue(sessionId, out SessionLease? lease))
        {
            return Result(SqlLabExecutionStatus.Refused, "La session SQL est inconnue ou déjà détruite.", diagnosticId, stopwatch);
        }

        bool executionGateHeld = false;
        bool sessionGateHeld = false;
        try
        {
            await _executionGate.WaitAsync(cancellationToken);
            executionGateHeld = true;
            await lease.Gate.WaitAsync(cancellationToken);
            sessionGateHeld = true;
            if (!_sessions.TryGetValue(sessionId, out SessionLease? current) || !ReferenceEquals(lease, current))
            {
                return Result(SqlLabExecutionStatus.Refused, "La session SQL a été réinitialisée.", diagnosticId, stopwatch);
            }

            await using SqlConnection connection = await OpenLabConnectionAsync(lease, cancellationToken);
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            try
            {
                await ApplySessionLimitsAsync(connection, transaction, cancellationToken);
                SqlLabResultSet resultSet = await ExecuteResultSetAsync(
                    connection,
                    transaction,
                    query,
                    cancellationToken);
                long orderCount = await ReadOrderCountAsync(connection, transaction, cancellationToken);
                await transaction.RollbackAsync(CancellationToken.None);
                SqlLabValidationResult? validation = expectation is null
                    ? null
                    : SqlResultValidator.Validate(expectation, resultSet);
                return new SqlLabExecutionResult(
                    SqlLabExecutionStatus.Succeeded,
                    resultSet,
                    [new SqlLabEffectResult("OrdersCount (transaction)", orderCount.ToString(CultureInfo.InvariantCulture))],
                    validation,
                    validation is null
                        ? "Requête exécutée puis transaction annulée."
                        : validation.Passed
                            ? "Résultat conforme ; transaction annulée."
                            : "Requête exécutée ; le résultat ne correspond pas encore à la cible.",
                    diagnosticId,
                    stopwatch.Elapsed);
            }
            catch
            {
                await TryRollbackAsync(transaction);
                throw;
            }
        }
        catch (SqlLabResultLimitException)
        {
            return Result(
                SqlLabExecutionStatus.ResultLimitExceeded,
                "Résultat refusé : la limite de lignes ou d’octets a été dépassée.",
                diagnosticId,
                stopwatch);
        }
        catch (SqlException) when (cancellationToken.IsCancellationRequested)
        {
            return Result(SqlLabExecutionStatus.Cancelled, "Requête annulée.", diagnosticId, stopwatch);
        }
        catch (SqlException exception) when (exception.Number == -2)
        {
            LogSqlFailure(diagnosticId, exception.Number, "timeout");
            return Result(SqlLabExecutionStatus.TimedOut, "Requête arrêtée après expiration du délai.", diagnosticId, stopwatch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result(SqlLabExecutionStatus.Cancelled, "Requête annulée.", diagnosticId, stopwatch);
        }
        catch (SqlException exception)
        {
            LogSqlFailure(diagnosticId, exception.Number, "sql-error");
            SqlLabExecutionStatus status = exception.Number is 229 or 262 or 2760 or 916 or 15247
                ? SqlLabExecutionStatus.Refused
                : SqlLabExecutionStatus.Failed;
            return Result(status, MapSqlError(exception.Number), diagnosticId, stopwatch);
        }
        finally
        {
            if (sessionGateHeld)
            {
                lease.Gate.Release();
            }

            if (executionGateHeld)
            {
                _executionGate.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (Guid sessionId in _sessions.Keys.ToArray())
        {
            try
            {
                await DestroySessionAsync(sessionId, CancellationToken.None);
            }
            catch (Exception exception) when (exception is SqlException or IOException)
            {
                LogCleanupFailure(_logger, "l’arrêt de l’hôte", exception);
            }
        }

        _lifecycleGate.Dispose();
        _executionGate.Dispose();
    }

    private async Task<SessionLease> ProvisionLeaseAsync(
        int generation,
        CancellationToken cancellationToken,
        Guid? existingId = null)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        string databaseName = $"forge_lab_{token}";
        string loginName = $"forge_user_{token}";
        string password = $"F!{Convert.ToHexString(RandomNumberGenerator.GetBytes(18))}a9";
        try
        {
            await using SqlConnection master = await OpenAdministratorConnectionAsync("master", cancellationToken);
            await ExecuteAdministratorCommandAsync(
                master,
                $"""
                CREATE DATABASE [{databaseName}];
                ALTER DATABASE [{databaseName}] SET TRUSTWORTHY OFF;
                ALTER DATABASE [{databaseName}] SET DB_CHAINING OFF;
                CREATE LOGIN [{loginName}]
                  WITH PASSWORD = N'{password}', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF,
                       DEFAULT_DATABASE = [{databaseName}];
                DENY VIEW SERVER STATE TO [{loginName}];
                DENY ALTER ANY LOGIN TO [{loginName}];
                DENY ALTER ANY DATABASE TO [{loginName}];
                """,
                cancellationToken);

            await using SqlConnection database = await OpenAdministratorConnectionAsync(databaseName, cancellationToken);
            await ExecuteAdministratorCommandAsync(database, SqlLabTemplate.SchemaAndDatasetSql, cancellationToken);
            await ExecuteAdministratorCommandAsync(
                database,
                $"""
                CREATE USER [{loginName}] FOR LOGIN [{loginName}];
                GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [{loginName}];
                DENY ALTER TO [{loginName}];
                DENY EXECUTE TO [{loginName}];
                DENY TAKE OWNERSHIP TO [{loginName}];
                DENY VIEW DEFINITION TO [{loginName}];
                """,
                cancellationToken);
        }
        catch
        {
            await CleanupNamesAsync(databaseName, loginName, CancellationToken.None);
            throw;
        }

        return new SessionLease(
            existingId ?? Guid.NewGuid(),
            generation,
            databaseName,
            loginName,
            password,
            _timeProvider.GetUtcNow());
    }

    private async Task CleanupLeaseAsync(SessionLease lease, CancellationToken cancellationToken) =>
        await CleanupNamesAsync(lease.DatabaseName, lease.LoginName, cancellationToken);

    private async Task CleanupNamesAsync(
        string databaseName,
        string loginName,
        CancellationToken cancellationToken)
    {
        await using SqlConnection master = await OpenAdministratorConnectionAsync("master", cancellationToken);
        await ExecuteAdministratorCommandAsync(
            master,
            $"""
            IF DB_ID(N'{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END;
            IF SUSER_ID(N'{loginName}') IS NOT NULL DROP LOGIN [{loginName}];
            """,
            cancellationToken);
    }

    private async Task<SqlConnection> OpenAdministratorConnectionAsync(
        string databaseName,
        CancellationToken cancellationToken)
    {
        string password = await ReadAdministratorSecretAsync(cancellationToken);
        var builder = CreateConnectionStringBuilder(databaseName, _options.AdministratorUser, password);
        builder.Pooling = false;
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private async Task<SqlConnection> OpenLabConnectionAsync(
        SessionLease lease,
        CancellationToken cancellationToken)
    {
        var builder = CreateConnectionStringBuilder(lease.DatabaseName, lease.LoginName, lease.Password);
        builder.Pooling = false;
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private SqlConnectionStringBuilder CreateConnectionStringBuilder(
        string databaseName,
        string user,
        string password) => new()
        {
            DataSource = $"{_options.Server},{_options.Port}",
            InitialCatalog = databaseName,
            UserID = user,
            Password = password,
            Encrypt = _options.Encrypt,
            TrustServerCertificate = _options.TrustServerCertificate,
            ConnectTimeout = _options.ConnectTimeoutSeconds,
            ApplicationName = "Forge.NET SqlLab",
            PersistSecurityInfo = false,
            MultipleActiveResultSets = false,
        };

    private async Task<string> ReadAdministratorSecretAsync(CancellationToken cancellationToken)
    {
        FileInfo file = new(_options.AdministratorPasswordFile);
        if (!file.Exists || file.Length is < 12 or > 512 || file.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("Le secret SqlLab est absent ou invalide.");
        }

        string secret = (await File.ReadAllTextAsync(file.FullName, Encoding.UTF8, cancellationToken)).Trim();
        if (secret.Length is < 12 or > 128 || secret.Contains('\0'))
        {
            throw new InvalidDataException("Le secret SqlLab est absent ou invalide.");
        }

        return secret;
    }

    private async Task ApplySessionLimitsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            "SET XACT_ABORT ON; SET LOCK_TIMEOUT 1000; SET DEADLOCK_PRIORITY LOW;",
            connection,
            transaction)
        {
            CommandTimeout = _options.QueryTimeoutSeconds,
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlLabResultSet> ExecuteResultSetAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string query,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(query, connection, transaction)
        {
            CommandTimeout = _options.QueryTimeoutSeconds,
        };
        using CancellationTokenRegistration cancellation = cancellationToken.Register(command.Cancel);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
        if (reader.FieldCount > 50)
        {
            command.Cancel();
            throw new SqlLabResultLimitException();
        }

        var columns = new List<SqlLabColumn>(reader.FieldCount);
        int bytes = 0;
        for (int index = 0; index < reader.FieldCount; index++)
        {
            string name = reader.GetName(index);
            bytes += Encoding.UTF8.GetByteCount(name);
            columns.Add(new SqlLabColumn(name, reader.GetDataTypeName(index), IsNullable: true));
        }

        var rows = new List<IReadOnlyList<SqlLabCell>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (rows.Count >= _options.MaximumRows)
            {
                command.Cancel();
                throw new SqlLabResultLimitException();
            }

            var row = new List<SqlLabCell>(reader.FieldCount);
            for (int index = 0; index < reader.FieldCount; index++)
            {
                if (await reader.IsDBNullAsync(index, cancellationToken))
                {
                    row.Add(new SqlLabCell(null, IsNull: true));
                    bytes += 4;
                    continue;
                }

                string value = NormalizeValue(reader.GetValue(index));
                bytes += Encoding.UTF8.GetByteCount(value);
                if (bytes > _options.MaximumResultBytes)
                {
                    command.Cancel();
                    throw new SqlLabResultLimitException();
                }

                row.Add(new SqlLabCell(value));
            }

            rows.Add(row);
        }

        return new SqlLabResultSet(columns, rows);
    }

    private async Task<long> ReadOrderCountAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.Orders;", connection, transaction)
        {
            CommandTimeout = _options.QueryTimeoutSeconds,
        };
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static async Task TryRollbackAsync(SqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            // La fermeture de la connexion abandonne encore la transaction ; ne pas masquer l'erreur initiale.
        }
    }

    private static async Task ExecuteAdministratorCommandAsync(
        SqlConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(commandText, connection)
        {
            CommandTimeout = 30,
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string NormalizeValue(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        decimal number => number.ToString("0.############################", CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        float number => number.ToString("R", CultureInfo.InvariantCulture),
        byte[] bytes => Convert.ToHexString(bytes),
        bool boolean => boolean ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static string MapSqlError(int number) => number switch
    {
        102 or 156 => "Syntaxe SQL invalide. Vérifiez l’instruction et les parenthèses.",
        207 => "Colonne inconnue dans le schéma visible.",
        208 => "Objet SQL inconnu dans le schéma visible.",
        229 or 262 or 2760 or 916 or 15247 => "Requête refusée par les droits minimaux de la session.",
        1205 => "La requête a été interrompue pour résoudre un verrouillage concurrent.",
        _ => "SQL Server a refusé la requête. Utilisez l’identifiant de diagnostic pour l’analyse locale.",
    };

    private static SqlLabExecutionResult Result(
        SqlLabExecutionStatus status,
        string message,
        Guid diagnosticId,
        Stopwatch stopwatch) => new(
            status,
            null,
            [],
            null,
            message,
            diagnosticId,
            stopwatch.Elapsed);

    private SqlLabSessionDescriptor Describe(SessionLease lease) => new(
        lease.Id,
        lease.Generation,
        lease.CreatedAtUtc,
        SqlLabTemplate.VisibleSchema,
        SqlLabTemplate.CreateLimits(_options));

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("SqlLab est désactivé. Démarrez le profil Compose sql-lab.");
        }
    }

    private void LogSqlFailure(Guid diagnosticId, int number, string category) =>
        LogSqlFailureMessage(_logger, category, diagnosticId, number, null);

    private sealed class SessionLease(
        Guid id,
        int generation,
        string databaseName,
        string loginName,
        string password,
        DateTimeOffset createdAtUtc)
    {
        public Guid Id { get; } = id;

        public int Generation { get; } = generation;

        public string DatabaseName { get; } = databaseName;

        public string LoginName { get; } = loginName;

        public string Password { get; } = password;

        public DateTimeOffset CreatedAtUtc { get; } = createdAtUtc;

        public SemaphoreSlim Gate { get; } = new(1, 1);
    }

    private sealed class SqlLabResultLimitException : Exception;
}
