using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

public sealed record SqlLabHomeView(
    bool Available,
    string StatusMessage,
    string SecurityNotice,
    string DefaultQuery);

public sealed record SqlLabSessionView(
    Guid Id,
    int Generation,
    DateTimeOffset CreatedAtUtc,
    string VisibleSchema,
    SqlLabLimits Limits);

public sealed record SqlLabRunView(
    SqlLabExecutionStatus Status,
    IReadOnlyList<SqlLabColumn> Columns,
    IReadOnlyList<IReadOnlyList<SqlLabCell>> Rows,
    IReadOnlyList<SqlLabEffectResult> Effects,
    SqlLabValidationResult? Validation,
    string Message,
    Guid DiagnosticId,
    long ElapsedMilliseconds);
