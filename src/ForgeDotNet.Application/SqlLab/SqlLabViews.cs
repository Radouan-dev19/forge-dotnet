using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

public sealed record SqlLabHomeView(
    bool Available,
    string StatusMessage,
    string SecurityNotice,
    string DefaultQuery,
    IReadOnlyList<SqlScenarioSummaryView> Scenarios);

public sealed record SqlScenarioSummaryView(
    string Id,
    string Title,
    int Difficulty,
    int EstimatedMinutes,
    IReadOnlyList<string> Skills);

/// <summary>
/// Énoncé public d'un scénario SQL.
/// </summary>
/// <remarks>
/// Seuls les noms de colonnes attendus sont exposés : ils font partie de la consigne. Les lignes de
/// référence et la solution restent côté serveur, sans quoi la validation ne prouverait plus rien.
/// </remarks>
public sealed record SqlScenarioView(
    string Id,
    int Version,
    string Title,
    int Difficulty,
    int EstimatedMinutes,
    IReadOnlyList<string> Skills,
    string Statement,
    string VisibleSchema,
    SqlLabLimits Limits,
    IReadOnlyList<string> EffectAssertions,
    IReadOnlyList<string> ExpectedColumns);

public sealed record SqlLabSessionView(
    Guid Id,
    int Generation,
    DateTimeOffset CreatedAtUtc,
    string VisibleSchema,
    SqlLabLimits Limits,
    string? ScenarioId);

public sealed record SqlLabRunView(
    SqlLabExecutionStatus Status,
    IReadOnlyList<SqlLabColumn> Columns,
    IReadOnlyList<IReadOnlyList<SqlLabCell>> Rows,
    IReadOnlyList<SqlLabEffectResult> Effects,
    SqlLabValidationResult? Validation,
    string Message,
    Guid DiagnosticId,
    long ElapsedMilliseconds);
