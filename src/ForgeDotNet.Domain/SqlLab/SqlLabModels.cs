namespace ForgeDotNet.Domain.SqlLab;

public enum SqlLabExecutionStatus
{
    Succeeded,
    Refused,
    TimedOut,
    Cancelled,
    ResultLimitExceeded,
    Failed,
    Unavailable,
}

public sealed record SqlLabLimits(
    int TimeoutSeconds,
    int MaximumRows,
    int MaximumResultBytes,
    int MaximumQueryCharacters);

public sealed record SqlLabColumn(string Name, string TypeName, bool IsNullable);

public sealed record SqlLabCell(string? Value, bool IsNull = false);

public sealed record SqlLabResultSet(
    IReadOnlyList<SqlLabColumn> Columns,
    IReadOnlyList<IReadOnlyList<SqlLabCell>> Rows);

public sealed record SqlLabExpectedResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<SqlLabCell>> Rows,
    bool Ordered,
    decimal NumericTolerance);

public sealed record SqlLabEffectResult(string Name, string Value);

public sealed record SqlLabValidationResult(bool Passed, IReadOnlyList<string> Issues);

public sealed record SqlLabExecutionResult(
    SqlLabExecutionStatus Status,
    SqlLabResultSet? Result,
    IReadOnlyList<SqlLabEffectResult> Effects,
    SqlLabValidationResult? Validation,
    string Message,
    Guid DiagnosticId,
    TimeSpan Elapsed);

public sealed record SqlLabSessionDescriptor(
    Guid Id,
    int Generation,
    DateTimeOffset CreatedAtUtc,
    string VisibleSchema,
    SqlLabLimits Limits);

public sealed record SqlLabAvailability(bool Available, string Message);
