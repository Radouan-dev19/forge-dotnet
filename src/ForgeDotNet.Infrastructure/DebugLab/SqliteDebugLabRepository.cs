using System.Text.Json;
using ForgeDotNet.Application.DebugLab;
using ForgeDotNet.Domain.DebugLab;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.DebugLab;

public sealed class SqliteDebugLabRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IDebugLabRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<DebugLabActivity?> GetAsync(
        Guid profileId,
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(profileId, scenarioId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DebugLabActivityRecord? record = await context.DebugLabActivities.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProfileId == profileId && item.ScenarioId == scenarioId, cancellationToken);
        return record is null ? null : await LoadAsync(context, record, cancellationToken);
    }

    public async ValueTask<DebugLabActivity> CreateOrGetAsync(
        DebugLabActivity activity,
        CancellationToken cancellationToken = default)
    {
        DebugLabRules.ValidateActivity(activity);
        if (activity.Version != 1 || activity.State != DebugLabState.InvestigationRequired || activity.Attempts.Count != 0)
        {
            throw new InvalidDataException("Une nouvelle activité DebugLab doit être vide et initiale.");
        }
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DebugLabActivityRecord? existing = await context.DebugLabActivities.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProfileId == activity.ProfileId && item.ScenarioId == activity.ScenarioId, cancellationToken);
        if (existing is not null) return await LoadAsync(context, existing, cancellationToken);
        context.DebugLabActivities.Add(ToRecord(activity));
        await context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async ValueTask<DebugLabActivity> SaveAsync(
        DebugLabActivity activity,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        DebugLabRules.ValidateActivity(activity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedVersion);
        if (activity.Version != expectedVersion + 1)
        {
            throw new InvalidDataException("Une mutation DebugLab doit créer exactement une version.");
        }
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DebugLabActivityRecord current = await context.DebugLabActivities
            .SingleOrDefaultAsync(item => item.Id == activity.Id, cancellationToken)
            ?? throw new KeyNotFoundException("L’activité DebugLab à enregistrer n’existe pas.");
        if (current.Version != expectedVersion || current.ProfileId != activity.ProfileId
            || current.ScenarioId != activity.ScenarioId || current.ScenarioVersion != activity.ScenarioVersion
            || current.ContentRevision != activity.ContentRevision)
        {
            throw new InvalidOperationException("L’activité DebugLab a changé ou sa révision ne correspond plus.");
        }

        DebugCorrectionAttemptRecord[] stored = await context.DebugCorrectionAttempts
            .Where(item => item.ActivityId == activity.Id).OrderBy(item => item.Sequence).ToArrayAsync(cancellationToken);
        if (stored.Length > activity.Attempts.Count)
        {
            throw new InvalidDataException("Une correction persistée ne peut pas être supprimée.");
        }
        for (int index = 0; index < stored.Length; index++)
        {
            DebugCorrectionAttempt expected = activity.Attempts[index];
            if (stored[index].Id != expected.Id || stored[index].SourceFingerprint != expected.SourceFingerprint
                || stored[index].Outcome != expected.Outcome || stored[index].DiagnosticId != expected.DiagnosticId)
            {
                throw new InvalidDataException("Une correction persistée ne peut pas être réécrite.");
            }
        }
        foreach (DebugCorrectionAttempt attempt in activity.Attempts.Skip(stored.Length))
        {
            context.DebugCorrectionAttempts.Add(new DebugCorrectionAttemptRecord
            {
                Id = attempt.Id,
                ActivityId = activity.Id,
                Sequence = attempt.Sequence,
                SourceFingerprint = attempt.SourceFingerprint,
                Outcome = attempt.Outcome,
                TotalTests = attempt.TotalTests,
                PassedTests = attempt.PassedTests,
                FailedTests = attempt.FailedTests,
                DiagnosticId = attempt.DiagnosticId,
                SubmittedAtUtc = attempt.SubmittedAtUtc,
            });
        }

        Apply(current, activity);
        await context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    private static async Task<DebugLabActivity> LoadAsync(
        ForgeDbContext context,
        DebugLabActivityRecord record,
        CancellationToken cancellationToken)
    {
        DebugCorrectionAttempt[] attempts = await context.DebugCorrectionAttempts.AsNoTracking()
            .Where(item => item.ActivityId == record.Id).OrderBy(item => item.Sequence)
            .Select(item => new DebugCorrectionAttempt(
                item.Id, item.Sequence, item.SourceFingerprint, item.Outcome, item.TotalTests,
                item.PassedTests, item.FailedTests, item.DiagnosticId, item.SubmittedAtUtc))
            .ToArrayAsync(cancellationToken);
        DebugRootCauseEvaluation? evaluation = record.EvaluationJson is null
            ? null
            : JsonSerializer.Deserialize<DebugRootCauseEvaluation>(record.EvaluationJson, JsonOptions)
                ?? throw new InvalidDataException("L’évaluation DebugLab persistée est illisible.");
        DebuggerObservations? observations = record.Breakpoint is null ? null : new DebuggerObservations(
            record.Breakpoint, record.Watch!, record.Locals!, record.CallStack!);
        var activity = new DebugLabActivity(
            record.Id, record.ProfileId, record.ScenarioId, record.ScenarioVersion, record.ContentRevision,
            record.Version, record.State, record.StartedAtUtc,
            new BugJournalEntry(record.Symptom, record.Context, record.Hypotheses, record.Evidence,
                record.Cause, record.Fix, record.Test, record.Prevention),
            observations, Array.AsReadOnly(attempts), evaluation, record.SolutionViewedAtUtc, record.CompletedAtUtc);
        DebugLabRules.ValidateActivity(activity);
        return activity;
    }

    private static DebugLabActivityRecord ToRecord(DebugLabActivity activity)
    {
        var record = new DebugLabActivityRecord
        {
            Id = activity.Id,
            ProfileId = activity.ProfileId,
            ScenarioId = activity.ScenarioId,
            ScenarioVersion = activity.ScenarioVersion,
            ContentRevision = activity.ContentRevision,
            StartedAtUtc = activity.StartedAtUtc,
        };
        Apply(record, activity);
        return record;
    }

    private static void Apply(DebugLabActivityRecord record, DebugLabActivity activity)
    {
        record.Version = activity.Version;
        record.State = activity.State;
        record.Symptom = activity.Journal.Symptom;
        record.Context = activity.Journal.Context;
        record.Hypotheses = activity.Journal.Hypotheses;
        record.Evidence = activity.Journal.Evidence;
        record.Cause = activity.Journal.Cause;
        record.Fix = activity.Journal.Fix;
        record.Test = activity.Journal.Test;
        record.Prevention = activity.Journal.Prevention;
        record.Breakpoint = activity.Observations?.Breakpoint;
        record.Watch = activity.Observations?.Watch;
        record.Locals = activity.Observations?.Locals;
        record.CallStack = activity.Observations?.CallStack;
        record.EvaluationJson = activity.Evaluation is null ? null : JsonSerializer.Serialize(activity.Evaluation, JsonOptions);
        record.SolutionViewedAtUtc = activity.SolutionViewedAtUtc;
        record.CompletedAtUtc = activity.CompletedAtUtc;
    }

    private static void ValidateKey(Guid profileId, string scenarioId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        if (string.IsNullOrWhiteSpace(scenarioId) || scenarioId.Length > 128)
        {
            throw new ArgumentException("L’identifiant DebugLab est invalide.", nameof(scenarioId));
        }
    }
}
