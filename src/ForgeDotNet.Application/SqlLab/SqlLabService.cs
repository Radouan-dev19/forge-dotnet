using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

/// <summary>
/// Parcours SQL : choix d'un scénario publié, session jetable dédiée, exécution bornée et
/// validation contre le résultat de référence du scénario.
/// </summary>
/// <remarks>
/// Le service portait auparavant une identité, une requête et une attente codées en dur. Les
/// quarante scénarios livrés n'étaient donc atteignables par aucun parcours utilisateur, et toute
/// tentative était enregistrée sous la même identité fictive — ce qui rendait la maîtrise SQL
/// inobservable. L'identité, la version et la révision proviennent désormais du scénario réel.
/// </remarks>
public sealed class SqlLabService
{
    private const string SandboxScenarioId = "sql-lab-reference-001";
    private const string SandboxContentRevision = "sql-lab-reference-v1";

    private readonly ISqlLabGateway _gateway;
    private readonly ISqlScenarioSource? _scenarioSource;
    private readonly ISqlLearningAttemptRepository? _attemptRepository;
    private readonly ILocalProfileRepository? _profileRepository;
    private readonly TimeProvider _timeProvider;

    public SqlLabService(
        ISqlLabGateway gateway,
        ISqlScenarioSource? scenarioSource = null,
        ISqlLearningAttemptRepository? attemptRepository = null,
        ILocalProfileRepository? profileRepository = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        _gateway = gateway;
        _scenarioSource = scenarioSource;
        _attemptRepository = attemptRepository;
        _profileRepository = profileRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public const string DefaultQuery = "SELECT OrderId, CustomerName, Total FROM dbo.Orders ORDER BY OrderId;";

    /// <summary>
    /// Résultat de référence du bac à sable technique, conservé pour que la base de démonstration
    /// reste vérifiable lorsqu'aucun scénario publié n'est sélectionné.
    /// </summary>
    private static readonly SqlLabExpectedResult SandboxExpectation = new(
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
        IReadOnlyList<SqlScenario> scenarios = _scenarioSource is null
            ? []
            : await _scenarioSource.ListAsync(cancellationToken);
        return new SqlLabHomeView(
            availability.Available,
            availability.Message,
            "Chaque exécution est bornée, exécutée avec un login minimal puis annulée dans une "
            + "transaction. La base peut être détruite à tout moment.",
            DefaultQuery,
            Array.AsReadOnly(scenarios
                .Select(scenario => new SqlScenarioSummaryView(
                    scenario.Id,
                    scenario.Title,
                    scenario.Difficulty,
                    scenario.EstimatedMinutes,
                    scenario.Skills))
                .ToArray()));
    }

    /// <summary>Énoncé public d'un scénario : jamais le résultat de référence ni la solution.</summary>
    public async Task<SqlScenarioView?> GetScenarioAsync(
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        SqlScenario? scenario = _scenarioSource is null
            ? null
            : await _scenarioSource.GetAsync(scenarioId, cancellationToken);
        return scenario is null
            ? null
            : new SqlScenarioView(
                scenario.Id,
                scenario.Version,
                scenario.Title,
                scenario.Difficulty,
                scenario.EstimatedMinutes,
                scenario.Skills,
                scenario.Statement,
                scenario.VisibleSchema,
                scenario.Limits,
                scenario.EffectAssertions,
                scenario.Expectation.Columns);
    }

    public async Task<SqlLabSessionView> CreateSessionAsync(
        string? scenarioId = null,
        CancellationToken cancellationToken = default)
    {
        SqlScenario? scenario = await ResolveScenarioAsync(scenarioId, cancellationToken);
        return Map(
            await _gateway.CreateSessionAsync(scenario?.ToProvisioning(), cancellationToken),
            scenario?.Id);
    }

    public async Task<SqlLabSessionView> ResetSessionAsync(
        Guid sessionId,
        string? scenarioId = null,
        CancellationToken cancellationToken = default) =>
        Map(await _gateway.ResetSessionAsync(sessionId, cancellationToken), scenarioId);

    public Task DestroySessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _gateway.DestroySessionAsync(sessionId, cancellationToken);

    public async Task<SqlLabRunView> ExecuteAsync(
        Guid sessionId,
        string query,
        bool validateReference,
        string? scenarioId = null,
        CancellationToken cancellationToken = default)
    {
        SqlScenario? scenario = await ResolveScenarioAsync(scenarioId, cancellationToken);
        SqlLabExecutionResult result = await _gateway.ExecuteAsync(
            sessionId,
            query,
            validateReference ? scenario?.Expectation ?? SandboxExpectation : null,
            cancellationToken);
        await RecordAttemptAsync(scenario, query, validateReference, result, cancellationToken);
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

    private async Task<SqlScenario?> ResolveScenarioAsync(
        string? scenarioId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scenarioId) || _scenarioSource is null)
        {
            return null;
        }

        return await _scenarioSource.GetAsync(scenarioId, cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario SQL « {scenarioId} » inconnu ou non publié.");
    }

    private async Task RecordAttemptAsync(
        SqlScenario? scenario,
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

        // L'identité tracée est celle du scénario réellement joué ; le bac à sable conserve la
        // sienne, distincte, pour qu'une preuve ne puisse jamais être attribuée au mauvais contenu.
        SqlLearningAttempt attempt = SqlLearningAttempt.Create(
            profile.LocalId,
            scenario?.Id ?? SandboxScenarioId,
            scenario?.Version ?? 1,
            scenario?.ContentRevision ?? SandboxContentRevision,
            result,
            validationRequested,
            fingerprint,
            _timeProvider.GetUtcNow());
        await _attemptRepository.AppendAsync(attempt, cancellationToken);
    }

    private static SqlLabSessionView Map(SqlLabSessionDescriptor session, string? scenarioId) => new(
        session.Id,
        session.Generation,
        session.CreatedAtUtc,
        session.VisibleSchema,
        session.Limits,
        scenarioId);
}
