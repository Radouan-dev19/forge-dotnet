using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.UnitTests;

public sealed class PracticeRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 26, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IncompleteReflectionIsRejected()
    {
        PracticeActivity activity = CreateActivity();
        PracticeReflection reflection = CompleteReflection() with { EdgeCases = "trop court" };

        Assert.Throws<ArgumentException>(() => PracticeRules.SaveReflection(activity, reflection));
    }

    [Fact]
    public void ReflectionIsFrozenAfterFirstHint()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = WithReflection(PracticeRules.Start(Guid.NewGuid(), exercise, Start));
        activity = PracticeRules.UseHint(activity, exercise, requestedLevel: 1, Start.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            PracticeRules.SaveReflection(activity, CompleteReflection(Start.AddMinutes(2))));
    }

    [Fact]
    public void HintRequiresReflectionAndStrictOrderWithCap()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = PracticeRules.Start(Guid.NewGuid(), exercise, Start);
        Assert.Throws<InvalidOperationException>(() =>
            PracticeRules.UseHint(activity, exercise, requestedLevel: 1, Start));

        activity = WithReflection(activity);
        Assert.Throws<InvalidOperationException>(() =>
            PracticeRules.UseHint(activity, exercise, requestedLevel: 2, Start));
        for (int level = 1; level <= 4; level++)
        {
            activity = PracticeRules.UseHint(activity, exercise, level, Start.AddMinutes(level));
        }

        Assert.Throws<InvalidOperationException>(() =>
            PracticeRules.UseHint(activity, exercise, requestedLevel: 5, Start.AddMinutes(5)));
    }

    [Fact]
    public void ManualDeclarationAndUsefulNotesAreRequiredForSeriousAttempt()
    {
        PracticeActivity activity = WithReflection(CreateActivity());
        activity = PracticeRules.SubmitAttempt(
            activity,
            new PracticeAttemptInput(LongAttempt("première"), "Observation manuelle suffisamment détaillée.", false),
            Start.AddMinutes(1));
        activity = PracticeRules.SubmitAttempt(
            activity,
            new PracticeAttemptInput(LongAttempt("deuxième"), "notes courtes", true),
            Start.AddMinutes(2));

        Assert.Equal(PracticeAttemptDecision.ManualCheckMissing, activity.Attempts[0].Decision);
        Assert.Equal(PracticeAttemptDecision.VerificationNotesTooShort, activity.Attempts[1].Decision);
        Assert.DoesNotContain(activity.Attempts, attempt => attempt.IsSerious);
    }

    [Fact]
    public void SubstantiallyDuplicateAttemptIsRecordedButNotSerious()
    {
        PracticeActivity activity = WithReflection(CreateActivity());
        string first = LongAttempt("utiliser decimal puis parcourir chaque montant et ajouter au total courant");
        activity = SubmitSerious(activity, first, Start.AddMinutes(1));
        activity = SubmitSerious(activity, first + " ", Start.AddMinutes(2));

        Assert.True(activity.Attempts[0].IsSerious);
        Assert.False(activity.Attempts[1].IsSerious);
        Assert.Equal(PracticeAttemptDecision.SubstantialDuplicate, activity.Attempts[1].Decision);
    }

    [Fact]
    public void SolutionRequiresTwoSeriousAttempts()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = WithReflection(PracticeRules.Start(Guid.NewGuid(), exercise, Start));
        activity = SubmitSerious(activity, LongAttempt("première stratégie"), Start.AddMinutes(1));

        PracticeSolutionEligibility eligibility = PracticeRules.GetSolutionEligibility(
            activity,
            exercise,
            Start.AddHours(1));

        Assert.False(eligibility.CanViewSolution);
        Assert.Equal(1, eligibility.SeriousAttemptCount);
        Assert.Throws<InvalidOperationException>(() =>
            PracticeRules.ViewSolution(activity, exercise, Start.AddHours(1)));
    }

    [Fact]
    public void ServerDelayStartsAtFirstSeriousAttempt()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = WithReflection(PracticeRules.Start(Guid.NewGuid(), exercise, Start));
        activity = SubmitSerious(activity, LongAttempt("première stratégie"), Start.AddMinutes(1));
        activity = SubmitSerious(activity, LongAttempt("seconde approche avec somme via boucle explicite"), Start.AddMinutes(2));

        PracticeSolutionEligibility early = PracticeRules.GetSolutionEligibility(activity, exercise, Start.AddMinutes(10));
        PracticeSolutionEligibility ready = PracticeRules.GetSolutionEligibility(activity, exercise, Start.AddMinutes(11));

        Assert.False(early.CanViewSolution);
        Assert.Equal(TimeSpan.FromMinutes(1), early.RemainingDelay);
        Assert.True(ready.CanViewSolution);
    }

    [Fact]
    public void ViewingSolutionExplicitlyKeepsActivityNonMastered()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = ReadyForSolution(exercise);

        activity = PracticeRules.ViewSolution(activity, exercise, Start.AddMinutes(11));

        Assert.Equal(PracticeActivityState.SolutionViewed, activity.State);
        Assert.NotNull(activity.SolutionViewedAtUtc);
        Assert.Null(activity.PostSolutionCompletedAtUtc);
    }

    [Fact]
    public void PersistedSolutionStateCannotBypassAttemptsAndServerDelay()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = WithReflection(PracticeRules.Start(Guid.NewGuid(), exercise, Start));
        PracticeActivity bypassed = activity with
        {
            State = PracticeActivityState.SolutionViewed,
            SolutionViewedAtUtc = Start.AddMinutes(1),
        };

        Assert.Throws<InvalidDataException>(() =>
            PracticeRules.GetSolutionEligibility(bypassed, exercise, Start.AddHours(1)));
    }

    [Fact]
    public void PersonalExplanationAndVariantAreRequiredAfterSolution()
    {
        PracticeExercise exercise = CreateExercise();
        PracticeActivity activity = PracticeRules.ViewSolution(
            ReadyForSolution(exercise),
            exercise,
            Start.AddMinutes(11));

        Assert.Throws<ArgumentException>(() => PracticeRules.CompletePostSolutionWork(
            activity,
            exercise,
            "trop court",
            LongAttempt("variante"),
            Start.AddMinutes(12)));

        PracticeActivity completed = PracticeRules.CompletePostSolutionWork(
            activity,
            exercise,
            LongAttempt("J'explique le choix décimal et la vérification des valeurs limites avec mes propres mots"),
            LongAttempt("Pour la variante je refuse les montants négatifs puis je conserve la même accumulation"),
            Start.AddMinutes(12));

        Assert.Equal(PracticeActivityState.PostSolutionCompleted, completed.State);
        Assert.NotNull(completed.PersonalExplanation);
        Assert.NotNull(completed.VariantSubmission);
    }

    [Fact]
    public void TextComparisonReportsSimilarityWithoutClaimingCorrectness()
    {
        PracticeTextComparison comparison = PracticeRules.Compare(
            LongAttempt("première approche distincte"),
            LongAttempt("autre approche avec agrégation"));

        Assert.False(comparison.IsSubstantialDuplicate);
        Assert.Contains("Aucun jugement de correction", comparison.Summary, StringComparison.Ordinal);
    }

    private static PracticeActivity CreateActivity() =>
        PracticeRules.Start(Guid.NewGuid(), CreateExercise(), Start);

    private static PracticeActivity WithReflection(PracticeActivity activity) =>
        PracticeRules.SaveReflection(activity, CompleteReflection());

    private static PracticeActivity SubmitSerious(
        PracticeActivity activity,
        string submission,
        DateTimeOffset at) => PracticeRules.SubmitAttempt(
            activity,
            new PracticeAttemptInput(submission, "Vérification manuelle : valeurs nominales et bornes relues sans résultat automatique.", true),
            at);

    private static PracticeActivity ReadyForSolution(PracticeExercise exercise)
    {
        PracticeActivity activity = WithReflection(PracticeRules.Start(Guid.NewGuid(), exercise, Start));
        activity = SubmitSerious(activity, LongAttempt("première stratégie"), Start.AddMinutes(1));
        return SubmitSerious(activity, LongAttempt("seconde approche avec une boucle et un cumul decimal"), Start.AddMinutes(2));
    }

    private static PracticeReflection CompleteReflection(DateTimeOffset? at = null) => new(
        "Je dois additionner chaque montant décimal sans perdre la précision métier attendue.",
        "Une collection locale de montants décimaux éventuellement vide.",
        "Un total décimal égal à la somme de tous les éléments fournis.",
        "Collection vide, valeur nulle interdite, montants négatifs et très grandes valeurs.",
        "Un accumulateur decimal évite la conversion binaire et conserve le comportement attendu.",
        "Initialiser le total à zéro, parcourir les éléments, additionner chacun puis retourner le cumul.",
        at ?? Start);

    private static PracticeExercise CreateExercise() => new(
        "exercise-001",
        1,
        new string('A', 64),
        "Calculer un total",
        1,
        20,
        "Calculer un total décimal.",
        Array.AsReadOnly(["Conserver la signature publique"]),
        Array.AsReadOnly([new PracticeExerciseExample("10, 5", "15")]),
        "public static class Submission { }",
        Array.AsReadOnly([
            new PracticeHint(1, "socratic", "Quelle valeur représente le cumul intermédiaire ?"),
            new PracticeHint(2, "location", "Regarde la boucle qui parcourt la collection."),
            new PracticeHint(3, "strategy", "Utilise un accumulateur decimal initialisé à zéro."),
            new PracticeHint(4, "partial-pseudocode", "Pour chaque montant, ajouter sa valeur au total courant."),
        ]),
        2,
        TimeSpan.FromMinutes(10),
        "La solution utilise un accumulateur decimal dans une boucle et retourne le total après le parcours.",
        "Le type decimal conserve la précision décimale attendue.",
        "exercise-002",
        "Calculer un total avec remise",
        "Calculer le total puis appliquer une remise bornée.");

    private static string LongAttempt(string distinctiveText) =>
        $"{distinctiveText}. Cette proposition décrit suffisamment les modifications prévues, les cas limites et le résultat manuel attendu pour être analysée.";
}
