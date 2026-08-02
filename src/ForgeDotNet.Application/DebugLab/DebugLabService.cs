using System.Globalization;
using System.Text;
using ForgeDotNet.Application.CodeRunner;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.DebugLab;

public sealed class DebugLabService(
    IDebugScenarioSource scenarioSource,
    IDebugLabRepository repository,
    ILocalProfileRepository profileRepository,
    ICodeRunner codeRunner,
    DebugLabCoordinator coordinator,
    TimeProvider timeProvider)
{
    public async ValueTask<IReadOnlyList<DebugScenarioSummaryView>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DebugScenario> scenarios = await scenarioSource.ListAsync(cancellationToken);
        return Array.AsReadOnly(scenarios
            .OrderBy(item => item.Difficulty)
            .ThenBy(item => item.Title, StringComparer.Ordinal)
            .Select(item => new DebugScenarioSummaryView(
                item.Id, item.Version, item.Title, item.Difficulty, item.EstimatedMinutes, item.Skills))
            .ToArray());
    }

    public async ValueTask<DebugLabActivityView> GetOrStartAsync(
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        DebugScenario scenario = await GetScenarioAsync(scenarioId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DebugLabActivity? existing = await repository.GetAsync(profile.LocalId, scenario.Id, cancellationToken);
        DebugLabActivity activity = existing ?? await repository.CreateOrGetAsync(
            DebugLabRules.Start(profile.LocalId, scenario, timeProvider.GetUtcNow()), cancellationToken);
        EnsureCurrentContent(activity, scenario);
        return ToView(activity, scenario);
    }

    public async ValueTask<DebugLabActivityView> SaveInvestigationAsync(
        string scenarioId,
        int expectedVersion,
        DebugInvestigationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await MutateAsync(scenarioId, expectedVersion, (activity, _, _) =>
            DebugLabRules.SaveInvestigation(
                activity,
                new BugJournalEntry(
                    input.Symptom, input.Context, input.Hypotheses, input.Evidence,
                    "", "", "", ""),
                new DebuggerObservations(input.Breakpoint, input.Watch, input.Locals, input.CallStack)), cancellationToken);
    }

    public async ValueTask<DebugLabActivityView> PrepareCorrectionAsync(
        string scenarioId,
        int expectedVersion,
        DebugCorrectionPreparationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await MutateAsync(scenarioId, expectedVersion, (activity, _, _) =>
            DebugLabRules.PrepareCorrection(activity, input.Fix, input.RegressionTest), cancellationToken);
    }

    public async ValueTask<DebugCorrectionRunResult> RunCorrectionAsync(
        string scenarioId,
        int expectedVersion,
        string source,
        CancellationToken cancellationToken = default)
    {
        DebugScenario scenario = await GetScenarioAsync(scenarioId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        DebugLabActivity current;
        await using (IAsyncDisposable lease = await coordinator.EnterAsync(cancellationToken))
        {
            current = await repository.GetAsync(profile.LocalId, scenario.Id, cancellationToken)
                ?? throw new KeyNotFoundException("L’activité DebugLab doit être ouverte avant la correction.");
            EnsureExpected(current, scenario, expectedVersion);
            if (current.State != DebugLabState.CorrectionReady)
            {
                throw new InvalidOperationException("L’investigation doit être complète avant l’exécution de la correction.");
            }
            if (current.Journal.Fix.Trim().Length < DebugLabRules.MinimumJournalFieldLength
                || current.Journal.Test.Trim().Length < DebugLabRules.MinimumJournalFieldLength)
            {
                throw new InvalidOperationException("La correction et son test de non-régression doivent être décrits avant l’exécution.");
            }
        }

        var request = new CodeRunRequest(
            Guid.NewGuid(), scenario.Id, scenario.Version, scenario.Revision,
            Array.AsReadOnly([new CodeRunSourceFile("Submission.cs", source)]));
        CodeRunContract.ValidateRequest(request);
        CodeRunResult runnerResult = CodeRunContract.NormalizeResult(
            request,
            await codeRunner.RunAsync(request, cancellationToken));

        await using (IAsyncDisposable lease = await coordinator.EnterAsync(cancellationToken))
        {
            DebugLabActivity latest = await repository.GetAsync(profile.LocalId, scenario.Id, cancellationToken)
                ?? throw new KeyNotFoundException("L’activité DebugLab a disparu pendant l’exécution.");
            EnsureExpected(latest, scenario, expectedVersion);
            DebugLabActivity updated = DebugLabRules.RecordCorrection(
                latest,
                source,
                MapOutcome(runnerResult.Status),
                runnerResult.Tests.TotalCount,
                runnerResult.Tests.PassedCount,
                runnerResult.Tests.FailedCount,
                runnerResult.DiagnosticId,
                runnerResult.CompletedAtUtc);
            DebugLabActivity saved = await repository.SaveAsync(updated, expectedVersion, cancellationToken);
            return new DebugCorrectionRunResult(ToView(saved, scenario), runnerResult);
        }
    }

    public ValueTask<DebugLabActivityView> CompleteAsync(
        string scenarioId,
        int expectedVersion,
        string cause,
        string prevention,
        CancellationToken cancellationToken = default) => MutateAsync(
            scenarioId, expectedVersion,
            (activity, scenario, now) => DebugLabRules.Complete(activity, scenario, cause, prevention, now),
            cancellationToken);

    public ValueTask<DebugLabActivityView> ViewSolutionAsync(
        string scenarioId,
        int expectedVersion,
        CancellationToken cancellationToken = default) => MutateAsync(
            scenarioId, expectedVersion,
            (activity, scenario, now) => DebugLabRules.ViewSolution(activity, scenario, now),
            cancellationToken);

    public async ValueTask<string> ExportJournalMarkdownAsync(
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        DebugLabActivityView view = await GetOrStartAsync(scenarioId, cancellationToken);
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"# Journal de bug — {view.Title}");
        text.AppendLine();
        text.AppendLine(CultureInfo.InvariantCulture, $"Scénario : `{view.ScenarioId}` v{view.ScenarioVersion}");
        text.AppendLine(CultureInfo.InvariantCulture, $"État : {view.StateLabel}");
        Add("Symptôme", view.Journal.Symptom);
        Add("Contexte", view.Journal.Context);
        Add("Hypothèses", view.Journal.Hypotheses);
        Add("Preuves", view.Journal.Evidence);
        Add("Cause", view.Journal.Cause);
        Add("Correction", view.Journal.Fix);
        Add("Test de non-régression", view.Journal.Test);
        Add("Prévention", view.Journal.Prevention);
        if (view.Observations is not null)
        {
            Add("Point d’arrêt", view.Observations.Breakpoint);
            Add("Watch", view.Observations.Watch);
            Add("Locals", view.Observations.Locals);
            Add("Call Stack", view.Observations.CallStack);
        }
        return text.ToString();

        void Add(string heading, string value)
        {
            text.AppendLine();
            text.AppendLine(CultureInfo.InvariantCulture, $"## {heading}");
            text.AppendLine();
            text.AppendLine(value);
        }
    }

    private async ValueTask<DebugLabActivityView> MutateAsync(
        string scenarioId,
        int expectedVersion,
        Func<DebugLabActivity, DebugScenario, DateTimeOffset, DebugLabActivity> transition,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        DebugScenario scenario = await GetScenarioAsync(scenarioId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DebugLabActivity current = await repository.GetAsync(profile.LocalId, scenario.Id, cancellationToken)
            ?? throw new KeyNotFoundException("L’activité DebugLab doit être ouverte avant cette action.");
        EnsureExpected(current, scenario, expectedVersion);
        DebugLabActivity updated = transition(current, scenario, timeProvider.GetUtcNow());
        DebugLabActivity saved = updated == current
            ? current
            : await repository.SaveAsync(updated, expectedVersion, cancellationToken);
        return ToView(saved, scenario);
    }

    private async ValueTask<DebugScenario> GetScenarioAsync(string scenarioId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scenarioId) || scenarioId.Length > 128)
        {
            throw new ArgumentException("L’identifiant de scénario est invalide.", nameof(scenarioId));
        }
        return await scenarioSource.GetAsync(scenarioId, cancellationToken)
            ?? throw new KeyNotFoundException("Le scénario DebugLab n’existe pas dans le catalogue publié.");
    }

    private static void EnsureExpected(DebugLabActivity activity, DebugScenario scenario, int expectedVersion)
    {
        EnsureCurrentContent(activity, scenario);
        if (activity.Version != expectedVersion)
        {
            throw new InvalidOperationException("L’activité a changé ; rechargez son état courant avant de continuer.");
        }
    }

    private static void EnsureCurrentContent(DebugLabActivity activity, DebugScenario scenario)
    {
        DebugLabRules.ValidateActivity(activity);
        if (activity.ScenarioId != scenario.Id || activity.ScenarioVersion != scenario.Version || activity.ContentRevision != scenario.Revision)
        {
            throw new InvalidDataException("Cette activité référence une autre révision du scénario.");
        }
    }

    private static DebugCorrectionOutcome MapOutcome(CodeRunStatus status) => status switch
    {
        CodeRunStatus.Succeeded => DebugCorrectionOutcome.Succeeded,
        CodeRunStatus.CompilationFailed => DebugCorrectionOutcome.CompilationFailed,
        CodeRunStatus.TestsFailed => DebugCorrectionOutcome.TestsFailed,
        CodeRunStatus.TimedOut => DebugCorrectionOutcome.TimedOut,
        CodeRunStatus.Cancelled => DebugCorrectionOutcome.Cancelled,
        CodeRunStatus.Unavailable => DebugCorrectionOutcome.Unavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static DebugLabActivityView ToView(DebugLabActivity activity, DebugScenario scenario)
    {
        DebugCorrectionAttemptView[] attempts = activity.Attempts.Select(item => new DebugCorrectionAttemptView(
            item.Sequence, item.Outcome, OutcomeLabel(item.Outcome), item.TotalTests, item.PassedTests,
            item.FailedTests, item.DiagnosticId.ToString("N")[..12], item.SubmittedAtUtc)).ToArray();
        bool correctionPrepared = activity.Journal.Fix.Trim().Length >= DebugLabRules.MinimumJournalFieldLength
            && activity.Journal.Test.Trim().Length >= DebugLabRules.MinimumJournalFieldLength;
        return new DebugLabActivityView(
            scenario.Id, scenario.Version, scenario.Revision, scenario.Title, scenario.Difficulty,
            scenario.EstimatedMinutes, scenario.Skills, scenario.Ticket, scenario.ExpectedBehavior,
            scenario.SanitizedLogs, scenario.Checklist, scenario.ObservationQuestions, scenario.BrokenSource,
            scenario.RegressionTest, activity.Version, activity.State, StateLabel(activity.State),
            activity.Journal, activity.Observations, Array.AsReadOnly(attempts),
            activity.Evaluation?.Results ?? Array.Empty<DebugRubricResult>(),
            activity.State == DebugLabState.InvestigationRequired,
            activity.State == DebugLabState.CorrectionReady,
            activity.State == DebugLabState.CorrectionReady && correctionPrepared,
            activity.State == DebugLabState.RootCauseRequired,
            activity.State == DebugLabState.CorrectionReady
                && activity.Attempts.Count(item => item.Outcome != DebugCorrectionOutcome.Succeeded) >= DebugLabRules.SolutionAttemptThreshold,
            activity.State == DebugLabState.SolutionViewed ? scenario.CorrectionSource : null,
            activity.StartedAtUtc, activity.SolutionViewedAtUtc, activity.CompletedAtUtc);
    }

    private static string StateLabel(DebugLabState state) => state switch
    {
        DebugLabState.InvestigationRequired => "Investigation requise",
        DebugLabState.CorrectionReady => "Correction et test à préparer",
        DebugLabState.RootCauseRequired => "Cause racine à démontrer",
        DebugLabState.Completed => "Cycle de débogage terminé",
        DebugLabState.SolutionViewed => "Solution consultée — scénario non terminé",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    private static string OutcomeLabel(DebugCorrectionOutcome outcome) => outcome switch
    {
        DebugCorrectionOutcome.Succeeded => "Correction et régression réussies",
        DebugCorrectionOutcome.CompilationFailed => "Compilation échouée",
        DebugCorrectionOutcome.TestsFailed => "Test de non-régression échoué",
        DebugCorrectionOutcome.TimedOut => "Délai dépassé",
        DebugCorrectionOutcome.Cancelled => "Exécution annulée",
        DebugCorrectionOutcome.Unavailable => "Runner indisponible",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };
}
