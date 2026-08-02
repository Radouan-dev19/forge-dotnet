using System.Security.Cryptography;
using System.Text;

namespace ForgeDotNet.Domain.Practice;

public static class PracticeRules
{
    public const int MinimumReformulationLength = 20;
    public const int MinimumInputsLength = 10;
    public const int MinimumExpectedOutputLength = 10;
    public const int MinimumEdgeCasesLength = 20;
    public const int MinimumHypothesisLength = 20;
    public const int MinimumPlanLength = 30;
    public const int MaximumReflectionFieldLength = 4_000;
    public const int MinimumAttemptLength = 20;
    public const int MinimumSeriousAttemptLength = 80;
    public const int MaximumAttemptLength = 20_000;
    public const int MinimumVerificationNotesLength = 20;
    public const int MaximumVerificationNotesLength = 4_000;
    public const int MinimumPostSolutionLength = 80;
    public const int MaximumPostSolutionLength = 8_000;
    public const int HintCount = 4;
    public const int RequiredSeriousAttempts = 2;

    public static PracticeActivity Start(
        Guid profileId,
        PracticeExercise exercise,
        DateTimeOffset startedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ValidateExercise(exercise);
        ValidateUtc(startedAtUtc, nameof(startedAtUtc));
        var activity = new PracticeActivity(
            Guid.NewGuid(),
            profileId,
            exercise.Id,
            exercise.Version,
            exercise.Revision,
            Version: 1,
            PracticeActivityState.ReflectionRequired,
            startedAtUtc,
            Reflection: null,
            Attempts: Array.Empty<PracticeAttempt>(),
            HintUsages: Array.Empty<PracticeHintUsage>(),
            SolutionViewedAtUtc: null,
            PersonalExplanation: null,
            VariantSubmission: null,
            PostSolutionCompletedAtUtc: null);
        ValidateActivity(activity);
        return activity;
    }

    public static PracticeActivity SaveReflection(
        PracticeActivity activity,
        PracticeReflection reflection)
    {
        ValidateActivity(activity);
        ValidateReflection(reflection);
        if (activity.State is PracticeActivityState.SolutionViewed or PracticeActivityState.PostSolutionCompleted)
        {
            throw new InvalidOperationException("La réflexion ne peut plus être modifiée après consultation de la solution.");
        }

        if (activity.Attempts.Count > 0 || activity.HintUsages.Count > 0)
        {
            throw new InvalidOperationException("La réflexion est figée dès la première tentative ou le premier indice.");
        }

        PracticeActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = PracticeActivityState.Attempting,
            Reflection = reflection,
        };
        ValidateActivity(updated);
        return updated;
    }

    public static PracticeActivity SubmitAttempt(
        PracticeActivity activity,
        PracticeAttemptInput input,
        DateTimeOffset submittedAtUtc)
    {
        ValidateActivity(activity);
        ArgumentNullException.ThrowIfNull(input);
        ValidateUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (activity.State != PracticeActivityState.Attempting || activity.Reflection is null)
        {
            throw new InvalidOperationException("Une réflexion complète est requise avant toute tentative.");
        }

        string submission = NormalizeRequiredInput(
            input.SubmissionText,
            MinimumAttemptLength,
            MaximumAttemptLength,
            "La proposition de tentative");
        string verificationNotes = NormalizeOptionalInput(
            input.ManualVerificationNotes,
            MaximumVerificationNotesLength,
            "Les notes de vérification manuelle");
        PracticeAttemptDecision decision = EvaluateAttempt(
            activity.Attempts,
            submission,
            verificationNotes,
            input.ManualCheckDeclared);
        string fingerprint = Fingerprint(submission);
        var attempt = new PracticeAttempt(
            Guid.NewGuid(),
            activity.Attempts.Count + 1,
            submission,
            verificationNotes,
            input.ManualCheckDeclared,
            decision == PracticeAttemptDecision.Serious,
            decision,
            fingerprint,
            submittedAtUtc);
        PracticeActivity updated = activity with
        {
            Version = activity.Version + 1,
            Attempts = Array.AsReadOnly(activity.Attempts.Append(attempt).ToArray()),
        };
        ValidateActivity(updated);
        return updated;
    }

    public static PracticeActivity UseHint(
        PracticeActivity activity,
        PracticeExercise exercise,
        int requestedLevel,
        DateTimeOffset usedAtUtc)
    {
        ValidateActivityAgainstExercise(activity, exercise);
        ValidateUtc(usedAtUtc, nameof(usedAtUtc));
        if (activity.State != PracticeActivityState.Attempting || activity.Reflection is null)
        {
            throw new InvalidOperationException("Une réflexion complète est requise avant tout indice.");
        }

        int expectedLevel = activity.HintUsages.Count + 1;
        if (requestedLevel != expectedLevel || requestedLevel is < 1 or > HintCount)
        {
            throw new InvalidOperationException("Les quatre indices doivent être débloqués une seule fois et dans l'ordre.");
        }

        PracticeHint hint = exercise.Hints[requestedLevel - 1];
        var usage = new PracticeHintUsage(Guid.NewGuid(), hint.Level, hint.Kind, usedAtUtc);
        PracticeActivity updated = activity with
        {
            Version = activity.Version + 1,
            HintUsages = Array.AsReadOnly(activity.HintUsages.Append(usage).ToArray()),
        };
        ValidateActivity(updated);
        return updated;
    }

    public static PracticeSolutionEligibility GetSolutionEligibility(
        PracticeActivity activity,
        PracticeExercise exercise,
        DateTimeOffset nowUtc)
    {
        ValidateActivityAgainstExercise(activity, exercise);
        ValidateUtc(nowUtc, nameof(nowUtc));
        int seriousCount = activity.Attempts.Count(attempt => attempt.IsSerious);
        if (activity.State is PracticeActivityState.SolutionViewed or PracticeActivityState.PostSolutionCompleted)
        {
            return new PracticeSolutionEligibility(
                true,
                seriousCount,
                exercise.RequiredSeriousAttempts,
                activity.SolutionViewedAtUtc,
                TimeSpan.Zero,
                "La solution a déjà été consultée.");
        }

        if (seriousCount < exercise.RequiredSeriousAttempts)
        {
            return new PracticeSolutionEligibility(
                false,
                seriousCount,
                exercise.RequiredSeriousAttempts,
                null,
                TimeSpan.Zero,
                $"{exercise.RequiredSeriousAttempts - seriousCount} tentative(s) sérieuse(s) supplémentaire(s) requise(s).");
        }

        DateTimeOffset firstSeriousAttemptAt = activity.Attempts
            .Where(attempt => attempt.IsSerious)
            .Min(attempt => attempt.SubmittedAtUtc);
        DateTimeOffset availableAt = firstSeriousAttemptAt + exercise.MinimumSolutionDelay;
        TimeSpan remaining = availableAt > nowUtc ? availableAt - nowUtc : TimeSpan.Zero;
        return new PracticeSolutionEligibility(
            remaining == TimeSpan.Zero,
            seriousCount,
            exercise.RequiredSeriousAttempts,
            availableAt,
            remaining,
            remaining == TimeSpan.Zero
                ? "Deux tentatives sérieuses et le délai serveur sont satisfaits."
                : "Le délai serveur après la première tentative sérieuse n'est pas encore écoulé.");
    }

    public static PracticeActivity ViewSolution(
        PracticeActivity activity,
        PracticeExercise exercise,
        DateTimeOffset viewedAtUtc)
    {
        PracticeSolutionEligibility eligibility = GetSolutionEligibility(activity, exercise, viewedAtUtc);
        if (activity.State is PracticeActivityState.SolutionViewed or PracticeActivityState.PostSolutionCompleted)
        {
            return activity;
        }

        if (!eligibility.CanViewSolution)
        {
            throw new InvalidOperationException(eligibility.Reason);
        }

        PracticeActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = PracticeActivityState.SolutionViewed,
            SolutionViewedAtUtc = viewedAtUtc,
        };
        ValidateActivity(updated);
        return updated;
    }

    public static PracticeActivity CompletePostSolutionWork(
        PracticeActivity activity,
        PracticeExercise exercise,
        string personalExplanation,
        string variantSubmission,
        DateTimeOffset completedAtUtc)
    {
        ValidateActivityAgainstExercise(activity, exercise);
        ValidateUtc(completedAtUtc, nameof(completedAtUtc));
        if (activity.State != PracticeActivityState.SolutionViewed)
        {
            throw new InvalidOperationException("La solution doit avoir été consultée avant ce travail de reprise.");
        }

        string explanation = NormalizeRequiredInput(
            personalExplanation,
            MinimumPostSolutionLength,
            MaximumPostSolutionLength,
            "L'explication personnelle");
        string variant = NormalizeRequiredInput(
            variantSubmission,
            MinimumPostSolutionLength,
            MaximumPostSolutionLength,
            "La proposition de variante");
        if (Compare(explanation, exercise.Solution).IsSubstantialDuplicate)
        {
            throw new InvalidOperationException("L'explication personnelle doit être reformulée et ne peut pas recopier la solution.");
        }

        if (Compare(variant, explanation).IsSubstantialDuplicate)
        {
            throw new InvalidOperationException("La variante doit proposer un travail distinct de l'explication personnelle.");
        }

        PracticeActivity updated = activity with
        {
            Version = activity.Version + 1,
            State = PracticeActivityState.PostSolutionCompleted,
            PersonalExplanation = explanation,
            VariantSubmission = variant,
            PostSolutionCompletedAtUtc = completedAtUtc,
        };
        ValidateActivity(updated);
        return updated;
    }

    public static PracticeTextComparison Compare(string current, string previous)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(previous);
        string normalizedCurrent = NormalizeForComparison(current);
        string normalizedPrevious = NormalizeForComparison(previous);
        if (normalizedCurrent.Length == 0 || normalizedPrevious.Length == 0)
        {
            return new PracticeTextComparison(0, false, "Comparaison insuffisante.");
        }

        string[] currentTokens = Tokenize(normalizedCurrent);
        string[] previousTokens = Tokenize(normalizedPrevious);
        var currentSet = currentTokens.ToHashSet(StringComparer.Ordinal);
        var previousSet = previousTokens.ToHashSet(StringComparer.Ordinal);
        int union = currentSet.Union(previousSet).Count();
        int intersection = currentSet.Intersect(previousSet).Count();
        int similarity = union == 0 ? 100 : (int)Math.Round(intersection * 100d / union);
        decimal lengthRatio = Math.Min(normalizedCurrent.Length, normalizedPrevious.Length)
            / (decimal)Math.Max(normalizedCurrent.Length, normalizedPrevious.Length);
        bool duplicate = string.Equals(normalizedCurrent, normalizedPrevious, StringComparison.Ordinal)
            || (similarity >= 90 && lengthRatio >= 0.8m);
        return new PracticeTextComparison(
            similarity,
            duplicate,
            duplicate
                ? $"Proposition substantiellement identique ({similarity} % de similarité lexicale)."
                : $"Évolution textuelle observée ({similarity} % de similarité lexicale). Aucun jugement de correction n'est produit.");
    }

    public static void ValidateActivity(PracticeActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Id == Guid.Empty
            || activity.ProfileId == Guid.Empty
            || string.IsNullOrWhiteSpace(activity.ExerciseId)
            || activity.ExerciseVersion < 1
            || activity.ContentRevision.Length != 64
            || activity.Version < 1)
        {
            throw new InvalidDataException("L'identité de l'activité de pratique est invalide.");
        }

        ValidateUtc(activity.StartedAtUtc, nameof(activity.StartedAtUtc));
        if (activity.Reflection is not null)
        {
            ValidateReflection(activity.Reflection);
        }

        if (activity.Attempts.Select(attempt => attempt.Id).Distinct().Count() != activity.Attempts.Count
            || activity.Attempts.Where((attempt, index) => attempt.Sequence != index + 1).Any()
            || activity.Attempts.Any(attempt =>
                attempt.Id == Guid.Empty
                || attempt.SubmissionFingerprint.Length != 64
                || !string.Equals(attempt.SubmissionFingerprint, Fingerprint(attempt.SubmissionText), StringComparison.Ordinal)
                || attempt.IsSerious != (attempt.Decision == PracticeAttemptDecision.Serious)
                || attempt.SubmissionText.Trim().Length < MinimumAttemptLength
                || attempt.SubmissionText.Length > MaximumAttemptLength
                || attempt.ManualVerificationNotes.Length > MaximumVerificationNotesLength
                || attempt.SubmittedAtUtc.Offset != TimeSpan.Zero))
        {
            throw new InvalidDataException("L'historique des tentatives est invalide.");
        }

        for (int index = 0; index < activity.Attempts.Count; index++)
        {
            PracticeAttempt attempt = activity.Attempts[index];
            PracticeAttemptDecision expectedDecision = EvaluateAttempt(
                activity.Attempts.Take(index).ToArray(),
                attempt.SubmissionText,
                attempt.ManualVerificationNotes,
                attempt.ManualCheckDeclared);
            if (attempt.Decision != expectedDecision)
            {
                throw new InvalidDataException("La qualification d'une tentative persistée est incohérente.");
            }
        }

        if (activity.HintUsages.Select(usage => usage.Id).Distinct().Count() != activity.HintUsages.Count
            || activity.HintUsages.Where((usage, index) => usage.Level != index + 1).Any()
            || activity.HintUsages.Count > HintCount
            || activity.HintUsages.Any(usage =>
                usage.Id == Guid.Empty
                || string.IsNullOrWhiteSpace(usage.Kind)
                || usage.UsedAtUtc.Offset != TimeSpan.Zero))
        {
            throw new InvalidDataException("L'historique des indices est invalide.");
        }

        bool solutionViewed = activity.State is PracticeActivityState.SolutionViewed or PracticeActivityState.PostSolutionCompleted;
        bool postCompleted = activity.State == PracticeActivityState.PostSolutionCompleted;
        if ((activity.Reflection is null) != (activity.State == PracticeActivityState.ReflectionRequired)
            || solutionViewed != (activity.SolutionViewedAtUtc is not null)
            || postCompleted != (activity.PostSolutionCompletedAtUtc is not null)
            || postCompleted != (activity.PersonalExplanation is not null && activity.VariantSubmission is not null)
            || (activity.PersonalExplanation is null) != (activity.VariantSubmission is null)
            || (!postCompleted && (activity.PersonalExplanation is not null || activity.VariantSubmission is not null)))
        {
            throw new InvalidDataException("L'état de l'activité de pratique est incohérent.");
        }
    }

    public static void ValidateExercise(PracticeExercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        if (string.IsNullOrWhiteSpace(exercise.Id)
            || exercise.Version < 1
            || exercise.Revision.Length != 64
            || string.IsNullOrWhiteSpace(exercise.Title)
            || exercise.Hints.Count != HintCount
            || exercise.Hints.Where((hint, index) => hint.Level != index + 1).Any()
            || exercise.RequiredSeriousAttempts != RequiredSeriousAttempts
            || exercise.MinimumSolutionDelay <= TimeSpan.Zero
            || string.IsNullOrWhiteSpace(exercise.Solution)
            || string.IsNullOrWhiteSpace(exercise.VariantStatement))
        {
            throw new InvalidDataException("Le contenu privé de l'exercice est invalide pour le protocole 04A.");
        }
    }

    private static PracticeAttemptDecision EvaluateAttempt(
        IReadOnlyList<PracticeAttempt> previousAttempts,
        string submission,
        string verificationNotes,
        bool manualCheckDeclared)
    {
        if (submission.Length < MinimumSeriousAttemptLength)
        {
            return PracticeAttemptDecision.SubmissionTooShort;
        }

        if (!manualCheckDeclared)
        {
            return PracticeAttemptDecision.ManualCheckMissing;
        }

        if (verificationNotes.Length < MinimumVerificationNotesLength)
        {
            return PracticeAttemptDecision.VerificationNotesTooShort;
        }

        return previousAttempts.Any(previous => Compare(submission, previous.SubmissionText).IsSubstantialDuplicate)
            ? PracticeAttemptDecision.SubstantialDuplicate
            : PracticeAttemptDecision.Serious;
    }

    private static void ValidateReflection(PracticeReflection reflection)
    {
        ArgumentNullException.ThrowIfNull(reflection);
        ValidateUtc(reflection.UpdatedAtUtc, nameof(reflection.UpdatedAtUtc));
        _ = NormalizeRequiredInput(reflection.Reformulation, MinimumReformulationLength, MaximumReflectionFieldLength, "La reformulation");
        _ = NormalizeRequiredInput(reflection.Inputs, MinimumInputsLength, MaximumReflectionFieldLength, "Les entrées");
        _ = NormalizeRequiredInput(reflection.ExpectedOutput, MinimumExpectedOutputLength, MaximumReflectionFieldLength, "La sortie attendue");
        _ = NormalizeRequiredInput(reflection.EdgeCases, MinimumEdgeCasesLength, MaximumReflectionFieldLength, "Les cas limites");
        _ = NormalizeRequiredInput(reflection.Hypothesis, MinimumHypothesisLength, MaximumReflectionFieldLength, "L'hypothèse");
        _ = NormalizeRequiredInput(reflection.Plan, MinimumPlanLength, MaximumReflectionFieldLength, "Le plan");
    }

    private static void ValidateActivityAgainstExercise(PracticeActivity activity, PracticeExercise exercise)
    {
        ValidateActivity(activity);
        ValidateExercise(exercise);
        if (!string.Equals(activity.ExerciseId, exercise.Id, StringComparison.Ordinal)
            || activity.ExerciseVersion != exercise.Version
            || !string.Equals(activity.ContentRevision, exercise.Revision, StringComparison.Ordinal))
        {
            throw new InvalidDataException("L'activité ne peut pas être réinterprétée avec une autre version de contenu.");
        }

        if (activity.HintUsages.Any(usage =>
            !string.Equals(usage.Kind, exercise.Hints[usage.Level - 1].Kind, StringComparison.Ordinal)))
        {
            throw new InvalidDataException("L'historique des indices ne correspond pas au contenu figé.");
        }

        if (activity.State is PracticeActivityState.SolutionViewed or PracticeActivityState.PostSolutionCompleted)
        {
            PracticeAttempt[] seriousAttempts = activity.Attempts.Where(attempt => attempt.IsSerious).ToArray();
            DateTimeOffset viewedAt = activity.SolutionViewedAtUtc!.Value;
            if (seriousAttempts.Length < exercise.RequiredSeriousAttempts
                || viewedAt < seriousAttempts.Min(attempt => attempt.SubmittedAtUtc) + exercise.MinimumSolutionDelay
                || (activity.PostSolutionCompletedAtUtc is not null && activity.PostSolutionCompletedAtUtc < viewedAt))
            {
                throw new InvalidDataException("La solution persistée ne respecte pas ses conditions de déverrouillage serveur.");
            }
        }
    }

    private static string NormalizeRequiredInput(string? value, int minimum, int maximum, string label)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < minimum || normalized.Length > maximum)
        {
            throw new ArgumentException($"{label} doit contenir entre {minimum} et {maximum} caractères.");
        }

        return normalized;
    }

    private static string NormalizeOptionalInput(string? value, int maximum, string label)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length > maximum)
        {
            throw new ArgumentException($"{label} ne peut pas dépasser {maximum} caractères.");
        }

        return normalized;
    }

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeForComparison(value))));

    private static string NormalizeForComparison(string value) =>
        string.Join(' ', Tokenize(value.ToLowerInvariant()));

    private static string[] Tokenize(string value) => value
        .Split(
            value.Where(character => !char.IsLetterOrDigit(character) && character != '_').Distinct().ToArray(),
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La date doit être exprimée en UTC.", parameterName);
        }
    }
}
