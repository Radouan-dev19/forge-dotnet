namespace ForgeDotNet.Domain.DebugLab;

public enum DebugLabState
{
    InvestigationRequired,
    CorrectionReady,
    RootCauseRequired,
    Completed,
    SolutionViewed,
}

public enum DebugCorrectionOutcome
{
    Succeeded,
    CompilationFailed,
    TestsFailed,
    TimedOut,
    Cancelled,
    Unavailable,
}

public sealed record BugJournalEntry(
    string Symptom,
    string Context,
    string Hypotheses,
    string Evidence,
    string Cause,
    string Fix,
    string Test,
    string Prevention);

public sealed record DebuggerObservations(
    string Breakpoint,
    string Watch,
    string Locals,
    string CallStack);

public sealed record DebugCorrectionAttempt(
    Guid Id,
    int Sequence,
    string SourceFingerprint,
    DebugCorrectionOutcome Outcome,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    Guid DiagnosticId,
    DateTimeOffset SubmittedAtUtc);

public sealed record DebugRubricCriterion(
    string Id,
    string Label,
    string JournalField,
    IReadOnlyList<string> RequiredTerms,
    int MinimumMatches);

public sealed record DebugRubricResult(
    string CriterionId,
    string Label,
    bool Passed,
    int MatchedTerms,
    int RequiredMatches);

public sealed record DebugRootCauseEvaluation(
    bool Passed,
    IReadOnlyList<DebugRubricResult> Results,
    DateTimeOffset EvaluatedAtUtc);

public sealed record DebugScenario(
    string Id,
    int Version,
    string Revision,
    string Title,
    int Difficulty,
    int EstimatedMinutes,
    IReadOnlyList<string> Skills,
    string Ticket,
    string ExpectedBehavior,
    string SanitizedLogs,
    IReadOnlyList<string> Checklist,
    IReadOnlyList<string> ObservationQuestions,
    string BrokenSource,
    string CorrectionSource,
    string RegressionTest,
    IReadOnlyList<DebugRubricCriterion> Rubric);

public sealed record DebugLabActivity(
    Guid Id,
    Guid ProfileId,
    string ScenarioId,
    int ScenarioVersion,
    string ContentRevision,
    int Version,
    DebugLabState State,
    DateTimeOffset StartedAtUtc,
    BugJournalEntry Journal,
    DebuggerObservations? Observations,
    IReadOnlyList<DebugCorrectionAttempt> Attempts,
    DebugRootCauseEvaluation? Evaluation,
    DateTimeOffset? SolutionViewedAtUtc,
    DateTimeOffset? CompletedAtUtc);
