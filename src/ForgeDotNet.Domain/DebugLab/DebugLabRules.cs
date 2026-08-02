using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ForgeDotNet.Domain.DebugLab;

public static class DebugLabRules
{
    public const int MinimumJournalFieldLength = 12;
    public const int MinimumObservationLength = 8;
    public const int MaximumTextLength = 8_000;
    public const int SolutionAttemptThreshold = 2;

    public static DebugLabActivity Start(Guid profileId, DebugScenario scenario, DateTimeOffset startedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ValidateScenario(scenario);
        ValidateUtc(startedAtUtc, nameof(startedAtUtc));
        var activity = new DebugLabActivity(
            Guid.NewGuid(), profileId, scenario.Id, scenario.Version, scenario.Revision, 1,
            DebugLabState.InvestigationRequired, startedAtUtc, EmptyJournal(), null,
            Array.Empty<DebugCorrectionAttempt>(), null, null, null);
        ValidateActivity(activity);
        return activity;
    }

    public static DebugLabActivity SaveInvestigation(
        DebugLabActivity activity,
        BugJournalEntry journal,
        DebuggerObservations observations)
    {
        ValidateActivity(activity);
        if (activity.State != DebugLabState.InvestigationRequired)
        {
            throw new InvalidOperationException("L’investigation est figée avant la première correction.");
        }

        ValidateRequired(journal.Symptom, nameof(journal.Symptom));
        ValidateRequired(journal.Context, nameof(journal.Context));
        ValidateRequired(journal.Hypotheses, nameof(journal.Hypotheses));
        ValidateRequired(journal.Evidence, nameof(journal.Evidence));
        ValidateOptionalConclusion(journal);
        ValidateObservation(observations.Breakpoint, nameof(observations.Breakpoint));
        ValidateObservation(observations.Watch, nameof(observations.Watch));
        ValidateObservation(observations.Locals, nameof(observations.Locals));
        ValidateObservation(observations.CallStack, nameof(observations.CallStack));

        DebugLabActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = DebugLabState.CorrectionReady,
            Journal = Normalize(journal),
            Observations = Normalize(observations),
        };
        ValidateActivity(updated);
        return updated;
    }

    public static DebugLabActivity PrepareCorrection(
        DebugLabActivity activity,
        string fix,
        string regressionTest)
    {
        ValidateActivity(activity);
        if (activity.State != DebugLabState.CorrectionReady)
        {
            throw new InvalidOperationException("La correction ne peut être préparée qu’après l’investigation.");
        }

        ValidateRequired(fix, nameof(fix));
        ValidateRequired(regressionTest, nameof(regressionTest));
        DebugLabActivity updated = activity with
        {
            Version = activity.Version + 1,
            Journal = activity.Journal with { Fix = fix.Trim(), Test = regressionTest.Trim() },
        };
        ValidateActivity(updated);
        return updated;
    }

    public static DebugLabActivity RecordCorrection(
        DebugLabActivity activity,
        string source,
        DebugCorrectionOutcome outcome,
        int totalTests,
        int passedTests,
        int failedTests,
        Guid diagnosticId,
        DateTimeOffset submittedAtUtc)
    {
        ValidateActivity(activity);
        if (activity.State != DebugLabState.CorrectionReady)
        {
            throw new InvalidOperationException("Une hypothèse et des preuves sont requises avant toute correction.");
        }

        ValidateRequired(activity.Journal.Fix, nameof(activity.Journal.Fix));
        ValidateRequired(activity.Journal.Test, nameof(activity.Journal.Test));
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Le code corrigé est obligatoire.", nameof(source));
        }

        if (diagnosticId == Guid.Empty || totalTests < 0 || passedTests < 0 || failedTests < 0
            || passedTests + failedTests > totalTests
            || (outcome == DebugCorrectionOutcome.Succeeded && (failedTests != 0 || totalTests == 0)))
        {
            throw new InvalidDataException("Le résultat de correction est incohérent.");
        }

        ValidateUtc(submittedAtUtc, nameof(submittedAtUtc));
        var attempt = new DebugCorrectionAttempt(
            Guid.NewGuid(), activity.Attempts.Count + 1, Fingerprint(source), outcome,
            totalTests, passedTests, failedTests, diagnosticId, submittedAtUtc);
        DebugLabActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = outcome == DebugCorrectionOutcome.Succeeded
                ? DebugLabState.RootCauseRequired
                : DebugLabState.CorrectionReady,
            Attempts = Array.AsReadOnly(activity.Attempts.Append(attempt).ToArray()),
        };
        ValidateActivity(updated);
        return updated;
    }

    public static DebugLabActivity Complete(
        DebugLabActivity activity,
        DebugScenario scenario,
        string cause,
        string prevention,
        DateTimeOffset completedAtUtc)
    {
        ValidateAgainstScenario(activity, scenario);
        if (activity.State != DebugLabState.RootCauseRequired
            || activity.Attempts.Count == 0
            || activity.Attempts[^1].Outcome != DebugCorrectionOutcome.Succeeded)
        {
            throw new InvalidOperationException("La correction et son test de non-régression doivent réussir avant la conclusion.");
        }

        ValidateRequired(cause, nameof(cause));
        ValidateRequired(prevention, nameof(prevention));
        ValidateUtc(completedAtUtc, nameof(completedAtUtc));
        BugJournalEntry journal = activity.Journal with { Cause = cause.Trim(), Prevention = prevention.Trim() };
        DebugRootCauseEvaluation evaluation = Evaluate(journal, scenario.Rubric, completedAtUtc);
        if (!evaluation.Passed)
        {
            throw new InvalidOperationException("La cause racine n’est pas suffisamment étayée par la grille du scénario.");
        }

        DebugLabActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = DebugLabState.Completed,
            Journal = journal,
            Evaluation = evaluation,
            CompletedAtUtc = completedAtUtc,
        };
        ValidateActivity(updated);
        return updated;
    }

    public static DebugLabActivity ViewSolution(
        DebugLabActivity activity,
        DebugScenario scenario,
        DateTimeOffset viewedAtUtc)
    {
        ValidateAgainstScenario(activity, scenario);
        ValidateUtc(viewedAtUtc, nameof(viewedAtUtc));
        if (activity.State == DebugLabState.SolutionViewed)
        {
            return activity;
        }

        if (activity.State != DebugLabState.CorrectionReady
            || activity.Attempts.Count(attempt => attempt.Outcome != DebugCorrectionOutcome.Succeeded) < SolutionAttemptThreshold)
        {
            throw new InvalidOperationException("Deux corrections échouées sont requises avant d’afficher la solution protégée.");
        }

        DebugLabActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = DebugLabState.SolutionViewed,
            SolutionViewedAtUtc = viewedAtUtc,
        };
        ValidateActivity(updated);
        return updated;
    }

    public static DebugRootCauseEvaluation Evaluate(
        BugJournalEntry journal,
        IReadOnlyList<DebugRubricCriterion> rubric,
        DateTimeOffset evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(rubric);
        ValidateUtc(evaluatedAtUtc, nameof(evaluatedAtUtc));
        DebugRubricResult[] results = rubric.Select(criterion =>
        {
            string value = Field(journal, criterion.JournalField);
            string normalized = NormalizeForMatch(value);
            int matches = criterion.RequiredTerms.Count(term => normalized.Contains(NormalizeForMatch(term), StringComparison.Ordinal));
            return new DebugRubricResult(
                criterion.Id, criterion.Label, matches >= criterion.MinimumMatches,
                matches, criterion.MinimumMatches);
        }).ToArray();
        return new DebugRootCauseEvaluation(results.All(result => result.Passed), Array.AsReadOnly(results), evaluatedAtUtc);
    }

    public static void ValidateScenario(DebugScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (string.IsNullOrWhiteSpace(scenario.Id) || scenario.Version < 1 || scenario.Revision.Length != 64
            || string.IsNullOrWhiteSpace(scenario.Title) || string.IsNullOrWhiteSpace(scenario.Ticket)
            || string.IsNullOrWhiteSpace(scenario.ExpectedBehavior) || string.IsNullOrWhiteSpace(scenario.BrokenSource)
            || string.IsNullOrWhiteSpace(scenario.CorrectionSource) || string.IsNullOrWhiteSpace(scenario.RegressionTest)
            || scenario.Checklist.Count < 3 || scenario.ObservationQuestions.Count < 2 || scenario.Rubric.Count < 2
            || scenario.Rubric.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Label)
                || item.JournalField is not ("cause" or "evidence" or "test" or "prevention")
                || item.RequiredTerms.Count == 0 || item.MinimumMatches < 1 || item.MinimumMatches > item.RequiredTerms.Count))
        {
            throw new InvalidDataException("Le scénario DebugLab est invalide.");
        }
    }

    public static void ValidateActivity(DebugLabActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Id == Guid.Empty || activity.ProfileId == Guid.Empty || string.IsNullOrWhiteSpace(activity.ScenarioId)
            || activity.ScenarioVersion < 1 || activity.ContentRevision.Length != 64 || activity.Version < 1)
        {
            throw new InvalidDataException("L’identité de l’activité DebugLab est invalide.");
        }

        ValidateUtc(activity.StartedAtUtc, nameof(activity.StartedAtUtc));
        if (activity.Attempts.Where((attempt, index) => attempt.Sequence != index + 1).Any()
            || activity.Attempts.Select(attempt => attempt.Id).Distinct().Count() != activity.Attempts.Count
            || activity.Attempts.Any(attempt => attempt.Id == Guid.Empty || attempt.DiagnosticId == Guid.Empty
                || attempt.SourceFingerprint.Length != 64 || attempt.SubmittedAtUtc.Offset != TimeSpan.Zero))
        {
            throw new InvalidDataException("L’historique DebugLab est invalide.");
        }

        bool investigated = activity.State != DebugLabState.InvestigationRequired;
        bool completed = activity.State == DebugLabState.Completed;
        bool solutionViewed = activity.State == DebugLabState.SolutionViewed;
        if (investigated != (activity.Observations is not null)
            || completed != (activity.CompletedAtUtc is not null && activity.Evaluation?.Passed == true)
            || solutionViewed != (activity.SolutionViewedAtUtc is not null)
            || (!completed && (activity.CompletedAtUtc is not null || activity.Evaluation is not null)))
        {
            throw new InvalidDataException("L’état de l’activité DebugLab est incohérent.");
        }
    }

    public static string Fingerprint(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));

    private static void ValidateAgainstScenario(DebugLabActivity activity, DebugScenario scenario)
    {
        ValidateActivity(activity);
        ValidateScenario(scenario);
        if (activity.ScenarioId != scenario.Id || activity.ScenarioVersion != scenario.Version || activity.ContentRevision != scenario.Revision)
        {
            throw new InvalidOperationException("L’activité ne correspond plus à la révision figée du scénario.");
        }
    }

    private static BugJournalEntry EmptyJournal() => new("", "", "", "", "", "", "", "");

    private static void ValidateOptionalConclusion(BugJournalEntry journal)
    {
        foreach (string value in new[] { journal.Cause, journal.Fix, journal.Test, journal.Prevention })
        {
            if (value.Length > MaximumTextLength)
            {
                throw new ArgumentException("Un champ du journal dépasse la limite autorisée.");
            }
        }
    }

    private static void ValidateRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < MinimumJournalFieldLength || value.Length > MaximumTextLength)
        {
            throw new ArgumentException($"Le champ {name} doit contenir une observation utile.", name);
        }
    }

    private static void ValidateObservation(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < MinimumObservationLength || value.Length > MaximumTextLength)
        {
            throw new ArgumentException($"L’observation {name} est insuffisante.", name);
        }
    }

    private static void ValidateUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("L’horodatage doit être en UTC.", name);
    }

    private static BugJournalEntry Normalize(BugJournalEntry value) => new(
        value.Symptom.Trim(), value.Context.Trim(), value.Hypotheses.Trim(), value.Evidence.Trim(),
        value.Cause.Trim(), value.Fix.Trim(), value.Test.Trim(), value.Prevention.Trim());

    private static DebuggerObservations Normalize(DebuggerObservations value) => new(
        value.Breakpoint.Trim(), value.Watch.Trim(), value.Locals.Trim(), value.CallStack.Trim());

    private static string Field(BugJournalEntry journal, string name) => name switch
    {
        "cause" => journal.Cause,
        "evidence" => journal.Evidence,
        "test" => journal.Test,
        "prevention" => journal.Prevention,
        _ => throw new InvalidDataException("Champ de grille DebugLab inconnu."),
    };

    private static string NormalizeForMatch(string value)
    {
        string decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray())
            .Normalize(NormalizationForm.FormC);
    }
}
