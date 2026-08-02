namespace ForgeDotNet.Domain.Practice;

public enum PracticeLearningAttemptStatus
{
    Succeeded,
    CompilationFailed,
    TestsFailed,
    TimedOut,
    Cancelled,
    Unavailable,
}

public sealed record PracticeLearningAttempt(
    Guid Id,
    Guid ProfileId,
    string ExerciseId,
    int ExerciseVersion,
    string ContentRevision,
    string SubmissionFingerprint,
    PracticeLearningAttemptStatus Status,
    int TotalTests,
    int PassedTests,
    Guid DiagnosticId,
    DateTimeOffset ObservedAtUtc)
{
    public void Validate()
    {
        if (Id == Guid.Empty
            || ProfileId == Guid.Empty
            || string.IsNullOrWhiteSpace(ExerciseId)
            || ExerciseId.Length > 100
            || ExerciseVersion < 1
            || string.IsNullOrWhiteSpace(ContentRevision)
            || ContentRevision.Length > 80
            || !IsSha256(SubmissionFingerprint)
            || TotalTests < 0
            || PassedTests < 0
            || PassedTests > TotalTests
            || DiagnosticId == Guid.Empty)
        {
            throw new InvalidOperationException("L’observation d’apprentissage C# est invalide.");
        }

        if (Status == PracticeLearningAttemptStatus.Succeeded
            && (TotalTests == 0 || PassedTests != TotalTests))
        {
            throw new InvalidOperationException("Une réussite C# doit être prouvée par des tests réussis.");
        }
    }

    private static bool IsSha256(string? value) => value is not null
        && value.Length == 71
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(char.IsAsciiHexDigit);
}
