using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

public sealed class SqlLabService
{
    private const string ReferenceScenarioId = "sql-lab-reference-001";
    private const string ReferenceContentRevision = "sql-lab-reference-v1";
    private readonly ISqlLabGateway _gateway;
    private readonly ISqlLearningAttemptRepository? _attemptRepository;
    private readonly ILocalProfileRepository? _profileRepository;
    private readonly TimeProvider _timeProvider;

    public SqlLabService(
        ISqlLabGateway gateway,
        ISqlLearningAttemptRepository? attemptRepository = null,
        ILocalProfileRepository? profileRepository = null,
        TimeProvider? timeProvider = null)
    {
        _gateway = gateway;
        _attemptRepository = attemptRepository;
        _profileRepository = profileRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public const string DefaultQuery = "SELECT OrderId, CustomerName, Total FROM dbo.Orders ORDER BY OrderId;";

    private static readonly SqlLabExpectedResult ReferenceExpectation = new(
        ["OrderId", "CustomerName", "Total"],
        [
            [new("1"), new("Ada"), new("120.50")],
            [new("2"), new("Grace"), new("75.00")],
            [new("3"), new("Linus"), new("40.25")],
        ],
        Ordered: true,
        NumericTolerance: 0.01m);

    public async Task<SqlLabHomeView> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        SqlLabAvailability availability = await _gateway.GetAvailabilityAsync(cancellationToken);
        return new SqlLabHomeView(
            availability.Available,
            availability.Message,
            "Chaque exécution est bornée, exécutée avec un login minimal puis annulée dans une transaction. La base peut être détruite à tout moment.",
            DefaultQuery);
    }

    public async Task<SqlLabSessionView> CreateSessionAsync(CancellationToken cancellationToken = default) =>
        Map(await _gateway.CreateSessionAsync(cancellationToken));

    public async Task<SqlLabSessionView> ResetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        Map(await _gateway.ResetSessionAsync(sessionId, cancellationToken));

    public Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _gateway.DestroySessionAsync(sessionId, cancellationToken);

    public async Task<SqlLabRunView> ExecuteAsync(
        Guid sessionId,
        string query,
        bool validateReference,
        CancellationToken cancellationToken = default)
    {
        SqlLabExecutionResult result = await _gateway.ExecuteAsync(
            sessionId,
            query,
            validateReference ? ReferenceExpectation : null,
            cancellationToken);
        await RecordAttemptAsync(query, validateReference, result, cancellationToken);
        return new SqlLabRunView(
            result.Status,
            result.Result?.Columns ?? [],
            result.Result?.Rows ?? [],
            result.Effects,
            result.Validation,
            result.Message,
            result.DiagnosticId,
            (long)result.Elapsed.TotalMilliseconds);
    }

    private async Task RecordAttemptAsync(
        string query,
        bool validationRequested,
        SqlLabExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (_attemptRepository is null || _profileRepository is null)
        {
            return;
        }

        var queryBytes = Encoding.UTF8.GetBytes(query);
        string fingerprint = Convert.ToHexStringLower(SHA256.HashData(queryBytes));
        var profile = await _profileRepository.GetAsync(cancellationToken);
        SqlLearningAttempt attempt = SqlLearningAttempt.Create(
            profile.LocalId,
            ReferenceScenarioId,
            1,
            ReferenceContentRevision,
            result,
            validationRequested,
            fingerprint,
            _timeProvider.GetUtcNow());
        await _attemptRepository.AppendAsync(attempt, cancellationToken);
    }

    private static SqlLabSessionView Map(SqlLabSessionDescriptor session) => new(
        session.Id,
        session.Generation,
        session.CreatedAtUtc,
        session.VisibleSchema,
        session.Limits);
}
