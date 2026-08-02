using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.Application.DebugLab;

public sealed record DebugScenarioSummaryView(
    string Id,
    int Version,
    string Title,
    int Difficulty,
    int EstimatedMinutes,
    IReadOnlyList<string> Skills);

public sealed record DebugInvestigationInput(
    string Symptom,
    string Context,
    string Hypotheses,
    string Evidence,
    string Breakpoint,
    string Watch,
    string Locals,
    string CallStack);

public sealed record DebugCorrectionPreparationInput(string Fix, string RegressionTest);

public sealed record DebugCorrectionAttemptView(
    int Sequence,
    DebugCorrectionOutcome Outcome,
    string OutcomeLabel,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    string DiagnosticReference,
    DateTimeOffset SubmittedAtUtc);

public sealed record DebugLabActivityView(
    string ScenarioId,
    int ScenarioVersion,
    string ContentRevision,
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
    string RegressionTestInstructions,
    int Version,
    DebugLabState State,
    string StateLabel,
    BugJournalEntry Journal,
    DebuggerObservations? Observations,
    IReadOnlyList<DebugCorrectionAttemptView> Attempts,
    IReadOnlyList<DebugRubricResult> EvaluationResults,
    bool CanInvestigate,
    bool CanPrepareCorrection,
    bool CanRunCorrection,
    bool CanComplete,
    bool CanViewSolution,
    string? ProtectedSolution,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SolutionViewedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record DebugCorrectionRunResult(DebugLabActivityView Activity, CodeRunResult RunnerResult);
