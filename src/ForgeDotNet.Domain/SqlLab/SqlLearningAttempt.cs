namespace ForgeDotNet.Domain.SqlLab;

public sealed record SqlLearningAttempt(
    Guid Id,
    Guid ProfileId,
    string ScenarioId,
    int ScenarioVersion,
    string ContentRevision,
    SqlLabExecutionStatus Status,
    bool ValidationRequested,
    bool? ValidationPassed,
    string QueryFingerprint,
    Guid DiagnosticId,
    DateTimeOffset ObservedAtUtc,
    long ElapsedMilliseconds)
{
    public static SqlLearningAttempt Create(
        Guid profileId,
        string scenarioId,
        int scenarioVersion,
        string contentRevision,
        SqlLabExecutionResult result,
        bool validationRequested,
        string queryFingerprint,
        DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (profileId == Guid.Empty
            || string.IsNullOrWhiteSpace(scenarioId)
            || scenarioId.Length > 128
            || scenarioVersion < 1
            || string.IsNullOrWhiteSpace(contentRevision)
            || contentRevision.Length > 80
            || queryFingerprint.Length != 64
            || result.DiagnosticId == Guid.Empty
            || observedAtUtc.Offset != TimeSpan.Zero
            || result.Elapsed < TimeSpan.Zero)
        {
            throw new ArgumentException("L’observation SQL est invalide.");
        }

        return new(
            Guid.NewGuid(),
            profileId,
            scenarioId,
            scenarioVersion,
            contentRevision,
            result.Status,
            validationRequested,
            result.Validation?.Passed,
            queryFingerprint,
            result.DiagnosticId,
            observedAtUtc,
            (long)result.Elapsed.TotalMilliseconds);
    }
}
