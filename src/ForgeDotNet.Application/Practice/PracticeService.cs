using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Application.Practice;

public sealed class PracticeService(
    IPracticeExerciseSource exerciseSource,
    IPracticeActivityRepository repository,
    ILocalProfileRepository profileRepository,
    PracticeCoordinator coordinator,
    TimeProvider timeProvider,
    IExamAccessPolicy examAccessPolicy)
{
    public async ValueTask<IReadOnlyList<PracticeExerciseSummaryView>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PracticeExercise> exercises = await exerciseSource.ListAsync(cancellationToken);
        return Array.AsReadOnly(exercises
            .OrderBy(exercise => exercise.Difficulty)
            .ThenBy(exercise => exercise.Title, StringComparer.Ordinal)
            .Select(exercise => new PracticeExerciseSummaryView(
                exercise.Id,
                exercise.Version,
                exercise.Title,
                exercise.Difficulty,
                exercise.EstimatedMinutes))
            .ToArray());
    }

    public async ValueTask<PracticeActivityView> GetOrStartAsync(
        string exerciseId,
        CancellationToken cancellationToken = default)
    {
        PracticeExercise exercise = await GetExerciseAsync(exerciseId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await EnsureAidUnlockedAsync(profile.LocalId, cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        PracticeActivity? existing = await repository.GetAsync(profile.LocalId, exercise.Id, cancellationToken);
        PracticeActivity activity = existing ?? await repository.CreateOrGetAsync(
            PracticeRules.Start(profile.LocalId, exercise, timeProvider.GetUtcNow()),
            cancellationToken);
        EnsureCurrentContent(activity, exercise);
        return ToView(activity, exercise, timeProvider.GetUtcNow());
    }

    public async ValueTask<PracticeActivityView> SaveReflectionAsync(
        string exerciseId,
        int expectedVersion,
        PracticeReflectionInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await MutateAsync(
            exerciseId,
            expectedVersion,
            (activity, _, now) => PracticeRules.SaveReflection(activity, new PracticeReflection(
                input.Reformulation,
                input.Inputs,
                input.ExpectedOutput,
                input.EdgeCases,
                input.Hypothesis,
                input.Plan,
                now)),
            cancellationToken);
    }

    public async ValueTask<PracticeActivityView> SubmitAttemptAsync(
        string exerciseId,
        int expectedVersion,
        PracticeAttemptInput input,
        CancellationToken cancellationToken = default) => await MutateAsync(
            exerciseId,
            expectedVersion,
            (activity, _, now) => PracticeRules.SubmitAttempt(activity, input, now),
            cancellationToken);

    public async ValueTask<PracticeActivityView> UnlockHintAsync(
        string exerciseId,
        int expectedVersion,
        int requestedLevel,
        CancellationToken cancellationToken = default) => await MutateAsync(
            exerciseId,
            expectedVersion,
            (activity, exercise, now) => PracticeRules.UseHint(activity, exercise, requestedLevel, now),
            cancellationToken);

    public async ValueTask<PracticeActivityView> ViewSolutionAsync(
        string exerciseId,
        int expectedVersion,
        CancellationToken cancellationToken = default) => await MutateAsync(
            exerciseId,
            expectedVersion,
            (activity, exercise, now) => PracticeRules.ViewSolution(activity, exercise, now),
            cancellationToken);

    public async ValueTask<PracticeActivityView> CompletePostSolutionWorkAsync(
        string exerciseId,
        int expectedVersion,
        string personalExplanation,
        string variantSubmission,
        CancellationToken cancellationToken = default) => await MutateAsync(
            exerciseId,
            expectedVersion,
            (activity, exercise, now) => PracticeRules.CompletePostSolutionWork(
                activity,
                exercise,
                personalExplanation,
                variantSubmission,
                now),
            cancellationToken);

    private async ValueTask<PracticeActivityView> MutateAsync(
        string exerciseId,
        int expectedVersion,
        Func<PracticeActivity, PracticeExercise, DateTimeOffset, PracticeActivity> transition,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        ArgumentNullException.ThrowIfNull(transition);
        PracticeExercise exercise = await GetExerciseAsync(exerciseId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await EnsureAidUnlockedAsync(profile.LocalId, cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        PracticeActivity current = await repository.GetAsync(profile.LocalId, exercise.Id, cancellationToken)
            ?? throw new KeyNotFoundException("L'activité de pratique doit être ouverte avant cette action.");
        EnsureCurrentContent(current, exercise);
        if (current.Version != expectedVersion)
        {
            throw new InvalidOperationException("L'activité a changé ; rechargez son état courant avant de continuer.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        PracticeActivity updated = transition(current, exercise, now);
        PracticeActivity saved = ReferenceEquals(updated, current) || updated == current
            ? current
            : await repository.SaveAsync(updated, expectedVersion, cancellationToken);
        return ToView(saved, exercise, now);
    }

    private async ValueTask<PracticeExercise> GetExerciseAsync(
        string exerciseId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(exerciseId) || exerciseId.Length > 128)
        {
            throw new ArgumentException("L'identifiant d'exercice est invalide.", nameof(exerciseId));
        }

        return await exerciseSource.GetAsync(exerciseId, cancellationToken)
            ?? throw new KeyNotFoundException("L'exercice n'existe pas dans le catalogue publié.");
    }

    private async ValueTask EnsureAidUnlockedAsync(Guid profileId, CancellationToken cancellationToken)
    {
        if (await examAccessPolicy.IsLearningAidLockedAsync(
            profileId,
            timeProvider.GetUtcNow(),
            cancellationToken))
        {
            throw new InvalidOperationException(
                "La pratique, ses indices et ses solutions sont verrouillés pendant l’examen sans aide actif.");
        }
    }

    private static void EnsureCurrentContent(PracticeActivity activity, PracticeExercise exercise)
    {
        PracticeRules.ValidateActivity(activity);
        if (!string.Equals(activity.ExerciseId, exercise.Id, StringComparison.Ordinal)
            || activity.ExerciseVersion != exercise.Version
            || !string.Equals(activity.ContentRevision, exercise.Revision, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Cette activité référence une autre version du contenu et ne peut pas être réinterprétée silencieusement.");
        }
    }

    private static PracticeActivityView ToView(
        PracticeActivity activity,
        PracticeExercise exercise,
        DateTimeOffset nowUtc)
    {
        PracticeSolutionEligibility eligibility = PracticeRules.GetSolutionEligibility(activity, exercise, nowUtc);
        PracticeAttemptView[] attempts = activity.Attempts.Select((attempt, index) =>
        {
            PracticeTextComparison? comparison = index == 0
                ? null
                : PracticeRules.Compare(attempt.SubmissionText, activity.Attempts[index - 1].SubmissionText);
            return new PracticeAttemptView(
                attempt.Sequence,
                attempt.SubmissionText,
                attempt.ManualVerificationNotes,
                attempt.ManualCheckDeclared,
                attempt.IsSerious,
                DecisionLabel(attempt.Decision),
                comparison?.SimilarityPercent,
                comparison?.Summary ?? "Première tentative : aucune comparaison antérieure.",
                attempt.SubmittedAtUtc);
        }).ToArray();
        PracticeHintUsageView[] hints = activity.HintUsages.Select(usage =>
        {
            PracticeHint hint = exercise.Hints[usage.Level - 1];
            return new PracticeHintUsageView(
                usage.Level,
                HintKindLabel(hint.Kind),
                hint.Content,
                usage.UsedAtUtc);
        }).ToArray();
        bool solutionViewed = activity.State is PracticeActivityState.SolutionViewed or PracticeActivityState.PostSolutionCompleted;
        return new PracticeActivityView(
            exercise.Id,
            exercise.Version,
            exercise.Revision,
            exercise.Title,
            exercise.Difficulty,
            exercise.EstimatedMinutes,
            exercise.Statement,
            exercise.Constraints,
            exercise.Examples,
            exercise.Starter,
            activity.Version,
            activity.State,
            StateLabel(activity.State),
            IsManualOnly: true,
            CanEditReflection: activity.Attempts.Count == 0
                && activity.HintUsages.Count == 0
                && !solutionViewed,
            CanSubmitAttempt: activity.State == PracticeActivityState.Attempting,
            CanUseHint: activity.State == PracticeActivityState.Attempting
                && activity.Reflection is not null
                && activity.HintUsages.Count < PracticeRules.HintCount,
            NextHintLevel: activity.HintUsages.Count < PracticeRules.HintCount
                ? activity.HintUsages.Count + 1
                : null,
            CanCompletePostSolutionWork: activity.State == PracticeActivityState.SolutionViewed,
            activity.Reflection is null ? null : new PracticeReflectionView(
                activity.Reflection.Reformulation,
                activity.Reflection.Inputs,
                activity.Reflection.ExpectedOutput,
                activity.Reflection.EdgeCases,
                activity.Reflection.Hypothesis,
                activity.Reflection.Plan,
                activity.Reflection.UpdatedAtUtc),
            Array.AsReadOnly(attempts),
            Array.AsReadOnly(hints),
            new PracticeSolutionEligibilityView(
                eligibility.CanViewSolution && activity.State == PracticeActivityState.Attempting,
                eligibility.SeriousAttemptCount,
                eligibility.RequiredSeriousAttempts,
                eligibility.AvailableAtUtc,
                (int)Math.Ceiling(eligibility.RemainingDelay.TotalSeconds),
                eligibility.Reason),
            solutionViewed ? exercise.Solution : null,
            solutionViewed ? exercise.Explanation : null,
            solutionViewed ? exercise.VariantId : null,
            solutionViewed ? exercise.VariantTitle : null,
            solutionViewed ? exercise.VariantStatement : null,
            activity.PersonalExplanation,
            activity.VariantSubmission,
            activity.StartedAtUtc,
            activity.SolutionViewedAtUtc,
            activity.PostSolutionCompletedAtUtc);
    }

    private static string StateLabel(PracticeActivityState state) => state switch
    {
        PracticeActivityState.ReflectionRequired => "Réflexion préalable requise",
        PracticeActivityState.Attempting => "Pratique manuelle en cours",
        PracticeActivityState.SolutionViewed => "Solution consultée — activité non maîtrisée",
        PracticeActivityState.PostSolutionCompleted => "Reprise renseignée — activité toujours non maîtrisée",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    private static string DecisionLabel(PracticeAttemptDecision decision) => decision switch
    {
        PracticeAttemptDecision.Serious => "Tentative sérieuse déclarée manuellement",
        PracticeAttemptDecision.SubmissionTooShort => "Proposition trop courte",
        PracticeAttemptDecision.ManualCheckMissing => "Vérification manuelle non déclarée",
        PracticeAttemptDecision.VerificationNotesTooShort => "Observation manuelle insuffisante",
        PracticeAttemptDecision.SubstantialDuplicate => "Doublon substantiel détecté",
        _ => throw new ArgumentOutOfRangeException(nameof(decision)),
    };

    private static string HintKindLabel(string kind) => kind switch
    {
        "socratic" => "Question socratique",
        "location" => "Localisation",
        "strategy" => "Stratégie",
        "partial-pseudocode" => "Pseudocode partiel",
        _ => throw new InvalidDataException("Le type d'indice privé est inconnu."),
    };
}
