using ForgeDotNet.Domain.DebugLab;

namespace ForgeDotNet.UnitTests;

public sealed class DebugLabRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CorrectionIsRefusedBeforeHypothesisEvidenceAndDebuggerObservations()
    {
        DebugLabActivity activity = DebugLabRules.Start(Guid.NewGuid(), Scenario(), Start);

        Assert.Throws<InvalidOperationException>(() => DebugLabRules.PrepareCorrection(
            activity, "Ajouter une garde explicite avant la déréférence.", "Tester le cas null avant et après la correction."));
        Assert.Throws<ArgumentException>(() => DebugLabRules.SaveInvestigation(
            activity,
            InvestigationJournal() with { Evidence = "trop court" },
            Observations()));
        Assert.Throws<ArgumentException>(() => DebugLabRules.SaveInvestigation(
            activity,
            InvestigationJournal(),
            Observations() with { Watch = "court" }));
    }

    [Fact]
    public void CorrectionWithoutRegressionTestIsRefused()
    {
        DebugLabActivity activity = Investigated();

        Assert.Throws<ArgumentException>(() => DebugLabRules.PrepareCorrection(
            activity, "Ajouter une garde explicite avant la déréférence.", "aucun"));
        Assert.Throws<ArgumentException>(() => DebugLabRules.RecordCorrection(
            activity, CorrectionSource, DebugCorrectionOutcome.Succeeded,
            6, 6, 0, Guid.NewGuid(), Start.AddMinutes(2)));
    }

    [Fact]
    public void SuccessfulCorrectionStillRequiresSupportedRootCause()
    {
        DebugScenario scenario = Scenario();
        DebugLabActivity activity = ReadyForRunner();
        activity = DebugLabRules.RecordCorrection(
            activity, CorrectionSource, DebugCorrectionOutcome.Succeeded,
            6, 6, 0, Guid.NewGuid(), Start.AddMinutes(3));

        Assert.Equal(DebugLabState.RootCauseRequired, activity.State);
        Assert.Throws<InvalidOperationException>(() => DebugLabRules.Complete(
            activity, scenario, "Une cause vague sans les termes observés.",
            "Relire le code avant la prochaine livraison.", Start.AddMinutes(4)));

        DebugLabActivity completed = DebugLabRules.Complete(
            activity,
            scenario,
            "La valeur null est déréférencée par Trim avant toute garde.",
            "Ajouter un test null obligatoire dans la revue de non-régression.",
            Start.AddMinutes(5));

        Assert.Equal(DebugLabState.Completed, completed.State);
        Assert.True(completed.Evaluation?.Passed);
        Assert.All(completed.Evaluation!.Results, result => Assert.True(result.Passed));
    }

    [Fact]
    public void FailedCorrectionKeepsInvestigationAndStoresOnlyFingerprint()
    {
        DebugLabActivity activity = ReadyForRunner();
        activity = DebugLabRules.RecordCorrection(
            activity, "public static class Submission { /* essai incorrect */ }",
            DebugCorrectionOutcome.TestsFailed, 6, 4, 2, Guid.NewGuid(), Start.AddMinutes(3));

        Assert.Equal(DebugLabState.CorrectionReady, activity.State);
        DebugCorrectionAttempt attempt = Assert.Single(activity.Attempts);
        Assert.Equal(64, attempt.SourceFingerprint.Length);
        Assert.DoesNotContain("Submission", attempt.SourceFingerprint, StringComparison.Ordinal);
        Assert.Equal(2, attempt.FailedTests);
    }

    [Fact]
    public void ProtectedSolutionRequiresTwoFailedCorrectionsAndNeverCompletesScenario()
    {
        DebugScenario scenario = Scenario();
        DebugLabActivity activity = ReadyForRunner();
        activity = DebugLabRules.RecordCorrection(
            activity, "first substantial correction source", DebugCorrectionOutcome.TestsFailed,
            6, 4, 2, Guid.NewGuid(), Start.AddMinutes(3));
        Assert.Throws<InvalidOperationException>(() => DebugLabRules.ViewSolution(activity, scenario, Start.AddMinutes(4)));
        activity = DebugLabRules.RecordCorrection(
            activity, "second substantial correction source", DebugCorrectionOutcome.CompilationFailed,
            0, 0, 0, Guid.NewGuid(), Start.AddMinutes(5));

        DebugLabActivity viewed = DebugLabRules.ViewSolution(activity, scenario, Start.AddMinutes(6));

        Assert.Equal(DebugLabState.SolutionViewed, viewed.State);
        Assert.Null(viewed.CompletedAtUtc);
        Assert.Null(viewed.Evaluation);
    }

    [Fact]
    public void RubricEvaluationIsDeterministicAndAccentInsensitive()
    {
        DebugRootCauseEvaluation evaluation = DebugLabRules.Evaluate(
            InvestigationJournal() with
            {
                Cause = "TRIM déréférence une valeur NULL.",
                Test = "Le test couvre explicitement la valeur absente.",
            },
            Scenario().Rubric,
            Start);

        Assert.True(evaluation.Passed);
        Assert.Equal(3, evaluation.Results.Count);
    }

    private static DebugLabActivity Investigated() => DebugLabRules.SaveInvestigation(
        DebugLabRules.Start(Guid.NewGuid(), Scenario(), Start), InvestigationJournal(), Observations());

    private static DebugLabActivity ReadyForRunner() => DebugLabRules.PrepareCorrection(
        Investigated(),
        "Ajouter une garde explicite avant Trim et conserver la normalisation.",
        "Tester la valeur null, la valeur absente et une valeur nominale.");

    private static BugJournalEntry InvestigationJournal() => new(
        "Une valeur absente provoque une NullReferenceException reproductible.",
        "L'import appelle FormatCustomerName avec un nom client absent.",
        "Trim pourrait déréférencer la valeur null avant la normalisation.",
        "La pile et la Watch montrent une valeur null au moment de Trim.",
        "", "", "", "");

    private static DebuggerObservations Observations() => new(
        "Arrêt placé sur l'appel à Trim.",
        "Watch value affiche null.",
        "Locals confirme le paramètre absent.",
        "Call Stack relie l'import à FormatCustomerName.");

    private static DebugScenario Scenario() => new(
        "debug-null-reference-001", 1, new string('A', 64), "Tracer une NullReferenceException",
        2, 35, Array.AsReadOnly(["debugging.null-reference"]),
        "Un import client échoue avec une valeur absente.",
        "Une valeur absente doit produire un résultat contrôlé.",
        "Event=CustomerImportFailed Exception=NullReferenceException",
        Array.AsReadOnly(["Reproduire le défaut", "Observer la valeur", "Tester la correction"]),
        Array.AsReadOnly(["Où la valeur devient-elle absente ?", "Quelle preuve distingue la cause ?"]),
        "public static class Submission { public static string FormatCustomerName(string value) => value.Trim(); }",
        CorrectionSource,
        "Tester null, blanc et un nom nominal.",
        Array.AsReadOnly([
            new DebugRubricCriterion("cause-null", "Cause null et Trim", "cause", Array.AsReadOnly(["null", "trim"]), 2),
            new DebugRubricCriterion("preuve-stack", "Preuve observée", "evidence", Array.AsReadOnly(["pile", "watch"]), 1),
            new DebugRubricCriterion("test-null", "Test absent", "test", Array.AsReadOnly(["null", "absente"]), 1),
        ]));

    private const string CorrectionSource =
        "public static class Submission { public static string FormatCustomerName(string value) => string.IsNullOrWhiteSpace(value) ? \"(inconnu)\" : value.Trim(); }";
}
