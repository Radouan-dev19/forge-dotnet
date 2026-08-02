using ForgeDotNet.Application.Practice;
using ForgeDotNet.Domain.Practice;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Practice;

public sealed class SqlitePracticeActivityRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IPracticeActivityRepository
{
    public async ValueTask<PracticeActivity?> GetAsync(
        Guid profileId,
        string exerciseId,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(profileId, exerciseId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        PracticeActivityRecord? activity = await context.PracticeActivities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ProfileId == profileId && item.ExerciseId == exerciseId,
                cancellationToken);
        return activity is null ? null : await LoadAggregateAsync(context, activity, cancellationToken);
    }

    public async ValueTask<PracticeActivity> CreateOrGetAsync(
        PracticeActivity activity,
        CancellationToken cancellationToken = default)
    {
        PracticeRules.ValidateActivity(activity);
        if (activity.Version != 1
            || activity.State != PracticeActivityState.ReflectionRequired
            || activity.Reflection is not null
            || activity.Attempts.Count > 0
            || activity.HintUsages.Count > 0)
        {
            throw new InvalidDataException("Une nouvelle activité de pratique doit être vide et à sa version initiale.");
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        PracticeActivityRecord? existing = await context.PracticeActivities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ProfileId == activity.ProfileId && item.ExerciseId == activity.ExerciseId,
                cancellationToken);
        if (existing is not null)
        {
            return await LoadAggregateAsync(context, existing, cancellationToken);
        }

        context.PracticeActivities.Add(ToRecord(activity));
        await context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async ValueTask<PracticeActivity> SaveAsync(
        PracticeActivity activity,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        PracticeRules.ValidateActivity(activity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        if (activity.Version != expectedVersion + 1)
        {
            throw new InvalidDataException("Une mutation doit créer exactement une nouvelle version d'activité.");
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        PracticeActivityRecord current = await context.PracticeActivities
            .SingleOrDefaultAsync(item => item.Id == activity.Id, cancellationToken)
            ?? throw new KeyNotFoundException("L'activité de pratique à enregistrer n'existe pas.");
        if (current.ProfileId != activity.ProfileId
            || !string.Equals(current.ExerciseId, activity.ExerciseId, StringComparison.Ordinal)
            || current.ExerciseVersion != activity.ExerciseVersion
            || !string.Equals(current.ContentRevision, activity.ContentRevision, StringComparison.Ordinal)
            || current.Version != expectedVersion)
        {
            throw new InvalidOperationException("L'activité de pratique a changé ou son contenu figé ne correspond plus.");
        }

        PracticeReflectionRecord? storedReflection = await context.PracticeReflections
            .SingleOrDefaultAsync(item => item.ActivityId == activity.Id, cancellationToken);
        SynchronizeReflection(context, activity, storedReflection);
        PracticeAttemptRecord[] storedAttempts = await context.PracticeAttempts
            .Where(item => item.ActivityId == activity.Id)
            .OrderBy(item => item.Sequence)
            .ToArrayAsync(cancellationToken);
        SynchronizeAttempts(context, activity, storedAttempts);
        PracticeHintUsageRecord[] storedHints = await context.PracticeHintUsages
            .Where(item => item.ActivityId == activity.Id)
            .OrderBy(item => item.Level)
            .ToArrayAsync(cancellationToken);
        SynchronizeHints(context, activity, storedHints);

        current.Version = activity.Version;
        current.State = activity.State;
        current.SolutionViewedAtUtc = activity.SolutionViewedAtUtc;
        current.PersonalExplanation = activity.PersonalExplanation;
        current.VariantSubmission = activity.VariantSubmission;
        current.PostSolutionCompletedAtUtc = activity.PostSolutionCompletedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    private static async Task<PracticeActivity> LoadAggregateAsync(
        ForgeDbContext context,
        PracticeActivityRecord activity,
        CancellationToken cancellationToken)
    {
        PracticeReflectionRecord? reflection = await context.PracticeReflections
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ActivityId == activity.Id, cancellationToken);
        PracticeAttempt[] attempts = await context.PracticeAttempts
            .AsNoTracking()
            .Where(item => item.ActivityId == activity.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new PracticeAttempt(
                item.Id,
                item.Sequence,
                item.SubmissionText,
                item.ManualVerificationNotes,
                item.ManualCheckDeclared,
                item.IsSerious,
                item.Decision,
                item.SubmissionFingerprint,
                item.SubmittedAtUtc))
            .ToArrayAsync(cancellationToken);
        PracticeHintUsage[] hints = await context.PracticeHintUsages
            .AsNoTracking()
            .Where(item => item.ActivityId == activity.Id)
            .OrderBy(item => item.Level)
            .Select(item => new PracticeHintUsage(item.Id, item.Level, item.Kind, item.UsedAtUtc))
            .ToArrayAsync(cancellationToken);
        var aggregate = new PracticeActivity(
            activity.Id,
            activity.ProfileId,
            activity.ExerciseId,
            activity.ExerciseVersion,
            activity.ContentRevision,
            activity.Version,
            activity.State,
            activity.StartedAtUtc,
            reflection is null ? null : new PracticeReflection(
                reflection.Reformulation,
                reflection.Inputs,
                reflection.ExpectedOutput,
                reflection.EdgeCases,
                reflection.Hypothesis,
                reflection.Plan,
                reflection.UpdatedAtUtc),
            Array.AsReadOnly(attempts),
            Array.AsReadOnly(hints),
            activity.SolutionViewedAtUtc,
            activity.PersonalExplanation,
            activity.VariantSubmission,
            activity.PostSolutionCompletedAtUtc);
        PracticeRules.ValidateActivity(aggregate);
        return aggregate;
    }

    private static void SynchronizeReflection(
        ForgeDbContext context,
        PracticeActivity activity,
        PracticeReflectionRecord? stored)
    {
        if (activity.Reflection is null)
        {
            if (stored is not null)
            {
                throw new InvalidDataException("Une réflexion persistée ne peut pas être supprimée.");
            }

            return;
        }

        if (stored is null)
        {
            context.PracticeReflections.Add(new PracticeReflectionRecord
            {
                ActivityId = activity.Id,
                Reformulation = activity.Reflection.Reformulation,
                Inputs = activity.Reflection.Inputs,
                ExpectedOutput = activity.Reflection.ExpectedOutput,
                EdgeCases = activity.Reflection.EdgeCases,
                Hypothesis = activity.Reflection.Hypothesis,
                Plan = activity.Reflection.Plan,
                UpdatedAtUtc = activity.Reflection.UpdatedAtUtc,
            });
            return;
        }

        stored.Reformulation = activity.Reflection.Reformulation;
        stored.Inputs = activity.Reflection.Inputs;
        stored.ExpectedOutput = activity.Reflection.ExpectedOutput;
        stored.EdgeCases = activity.Reflection.EdgeCases;
        stored.Hypothesis = activity.Reflection.Hypothesis;
        stored.Plan = activity.Reflection.Plan;
        stored.UpdatedAtUtc = activity.Reflection.UpdatedAtUtc;
    }

    private static void SynchronizeAttempts(
        ForgeDbContext context,
        PracticeActivity activity,
        PracticeAttemptRecord[] stored)
    {
        if (stored.Length > activity.Attempts.Count)
        {
            throw new InvalidDataException("Une tentative persistée ne peut pas être supprimée.");
        }

        for (int index = 0; index < stored.Length; index++)
        {
            PracticeAttempt expected = activity.Attempts[index];
            PracticeAttemptRecord actual = stored[index];
            if (actual.Id != expected.Id
                || actual.Sequence != expected.Sequence
                || !string.Equals(actual.SubmissionFingerprint, expected.SubmissionFingerprint, StringComparison.Ordinal)
                || actual.IsSerious != expected.IsSerious
                || actual.Decision != expected.Decision)
            {
                throw new InvalidDataException("Une tentative persistée ne peut pas être réécrite.");
            }
        }

        foreach (PracticeAttempt attempt in activity.Attempts.Skip(stored.Length))
        {
            context.PracticeAttempts.Add(new PracticeAttemptRecord
            {
                Id = attempt.Id,
                ActivityId = activity.Id,
                Sequence = attempt.Sequence,
                SubmissionText = attempt.SubmissionText,
                ManualVerificationNotes = attempt.ManualVerificationNotes,
                ManualCheckDeclared = attempt.ManualCheckDeclared,
                IsSerious = attempt.IsSerious,
                Decision = attempt.Decision,
                SubmissionFingerprint = attempt.SubmissionFingerprint,
                SubmittedAtUtc = attempt.SubmittedAtUtc,
            });
        }
    }

    private static void SynchronizeHints(
        ForgeDbContext context,
        PracticeActivity activity,
        PracticeHintUsageRecord[] stored)
    {
        if (stored.Length > activity.HintUsages.Count)
        {
            throw new InvalidDataException("Un indice persisté ne peut pas être supprimé.");
        }

        for (int index = 0; index < stored.Length; index++)
        {
            PracticeHintUsage expected = activity.HintUsages[index];
            PracticeHintUsageRecord actual = stored[index];
            if (actual.Id != expected.Id
                || actual.Level != expected.Level
                || !string.Equals(actual.Kind, expected.Kind, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Un usage d'indice persisté ne peut pas être réécrit.");
            }
        }

        foreach (PracticeHintUsage hint in activity.HintUsages.Skip(stored.Length))
        {
            context.PracticeHintUsages.Add(new PracticeHintUsageRecord
            {
                Id = hint.Id,
                ActivityId = activity.Id,
                Level = hint.Level,
                Kind = hint.Kind,
                UsedAtUtc = hint.UsedAtUtc,
            });
        }
    }

    private static PracticeActivityRecord ToRecord(PracticeActivity activity) => new()
    {
        Id = activity.Id,
        ProfileId = activity.ProfileId,
        ExerciseId = activity.ExerciseId,
        ExerciseVersion = activity.ExerciseVersion,
        ContentRevision = activity.ContentRevision,
        Version = activity.Version,
        State = activity.State,
        StartedAtUtc = activity.StartedAtUtc,
        SolutionViewedAtUtc = activity.SolutionViewedAtUtc,
        PersonalExplanation = activity.PersonalExplanation,
        VariantSubmission = activity.VariantSubmission,
        PostSolutionCompletedAtUtc = activity.PostSolutionCompletedAtUtc,
    };

    private static void ValidateKey(Guid profileId, string exerciseId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        if (string.IsNullOrWhiteSpace(exerciseId) || exerciseId.Length > 128)
        {
            throw new ArgumentException("L'identifiant d'exercice est invalide.", nameof(exerciseId));
        }
    }
}
