using System.Text.Json;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.Reviews;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Reviews;

public sealed class SqliteReviewRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IReviewRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<ReviewItem> CreateOrGetAsync(
        ReviewItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ReviewItemRecord? existing = await context.ReviewItems.AsNoTracking().SingleOrDefaultAsync(
            record => record.Id == item.Id,
            cancellationToken);
        if (existing is not null)
        {
            ReviewItem stored = ToDomain(existing);
            EnsureSameImmutableSource(stored, item);
            return stored;
        }

        context.ReviewItems.Add(ToRecord(item));
        await context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async ValueTask<ReviewItem?> GetAsync(
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(profileId, itemId);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ReviewItemRecord? record = await context.ReviewItems.AsNoTracking().SingleOrDefaultAsync(
            item => item.ProfileId == profileId && item.Id == itemId,
            cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async ValueTask<IReadOnlyList<ReviewItem>> ListActiveAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        ReviewItemRecord[] records = await context.ReviewItems.AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.IsActive)
            .OrderBy(item => item.DueOn)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(ToDomain).ToArray());
    }

    public async ValueTask SaveTransitionAsync(
        Guid profileId,
        int expectedVersion,
        ReviewTransition transition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ValidateKeys(profileId, transition.Item.Id);
        if (expectedVersion < 1
            || transition.Item.ProfileId != profileId
            || transition.Item.Version != expectedVersion + 1
            || transition.Attempt.ReviewItemId != transition.Item.Id
            || transition.Attempt.Sequence != transition.Item.AttemptCount)
        {
            throw new ArgumentException("La transition de révision est invalide.", nameof(transition));
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        ReviewItemRecord record = await context.ReviewItems.SingleOrDefaultAsync(
            item => item.ProfileId == profileId && item.Id == transition.Item.Id,
            cancellationToken)
            ?? throw new KeyNotFoundException("La carte de révision n’existe plus.");
        if (record.Version != expectedVersion)
        {
            throw new InvalidOperationException("La carte a déjà reçu une réponse concurrente.");
        }

        EnsureSameImmutableSource(ToDomain(record), transition.Item);
        record.CurrentIntervalIndex = transition.Item.CurrentIntervalIndex;
        record.DueOn = transition.Item.DueOn;
        record.AttemptCount = transition.Item.AttemptCount;
        record.Version = transition.Item.Version;
        record.IsActive = transition.Item.IsActive;
        record.LastReviewedAtUtc = transition.Item.LastReviewedAtUtc;
        context.ReviewAttempts.Add(ToRecord(transition.Attempt));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static ReviewItemRecord ToRecord(ReviewItem item) => new()
    {
        Id = item.Id,
        ProfileId = item.ProfileId,
        SourceKey = item.Source.Key,
        SourceKind = item.Source.Kind,
        SourceItemId = item.Source.ItemId,
        SourceItemVersion = item.Source.ItemVersion,
        SourceRevision = item.Source.Revision,
        SourceOccurredAtUtc = item.Source.OccurredAtUtc,
        Domain = item.Domain,
        ScheduleKind = item.ScheduleKind,
        Question = item.Card.Question,
        ExpectedAnswer = item.Card.ExpectedAnswer,
        ChoicesJson = JsonSerializer.Serialize(item.Card.Choices, JsonOptions),
        EvaluationMode = item.Card.EvaluationMode,
        CanProduceMasteryEvidence = item.Card.CanProduceMasteryEvidence,
        PolicyId = item.PolicyId,
        PolicyVersion = item.PolicyVersion,
        PolicyRevision = item.PolicyRevision,
        CurrentIntervalIndex = item.CurrentIntervalIndex,
        DueOn = item.DueOn,
        AttemptCount = item.AttemptCount,
        Version = item.Version,
        IsActive = item.IsActive,
        CreatedAtUtc = item.CreatedAtUtc,
        LastReviewedAtUtc = item.LastReviewedAtUtc,
    };

    private static ReviewAttemptRecord ToRecord(ReviewAttempt attempt) => new()
    {
        Id = attempt.Id,
        ReviewItemId = attempt.ReviewItemId,
        Sequence = attempt.Sequence,
        Outcome = attempt.Outcome,
        IsVerified = attempt.IsVerified,
        IsMasteryEligible = attempt.IsMasteryEligible,
        Score = attempt.Score,
        ResponseFingerprint = attempt.ResponseFingerprint,
        PreviousDueOn = attempt.PreviousDueOn,
        NextDueOn = attempt.NextDueOn,
        NextIntervalDays = attempt.NextIntervalDays,
        AnsweredAtUtc = attempt.AnsweredAtUtc,
    };

    private static ReviewItem ToDomain(ReviewItemRecord record)
    {
        ReviewChoice[] choices = JsonSerializer.Deserialize<ReviewChoice[]>(record.ChoicesJson, JsonOptions)
            ?? throw new InvalidDataException("Les choix privés de la carte sont illisibles.");
        return new ReviewItem(
            record.Id,
            record.ProfileId,
            new ReviewSource(
                record.SourceKey,
                record.SourceKind,
                record.SourceItemId,
                record.SourceItemVersion,
                record.SourceRevision,
                record.SourceOccurredAtUtc),
            record.Domain,
            record.ScheduleKind,
            new ReviewCard(
                record.Question,
                record.ExpectedAnswer,
                Array.AsReadOnly(choices),
                record.EvaluationMode,
                record.CanProduceMasteryEvidence),
            record.PolicyId,
            record.PolicyVersion,
            record.PolicyRevision,
            record.CurrentIntervalIndex,
            record.DueOn,
            record.AttemptCount,
            record.Version,
            record.IsActive,
            record.CreatedAtUtc,
            record.LastReviewedAtUtc);
    }

    private static void EnsureSameImmutableSource(ReviewItem stored, ReviewItem proposed)
    {
        if (stored.Id != proposed.Id
            || stored.ProfileId != proposed.ProfileId
            || stored.Source != proposed.Source
            || stored.Domain != proposed.Domain
            || stored.ScheduleKind != proposed.ScheduleKind
            || stored.Card.Question != proposed.Card.Question
            || stored.Card.ExpectedAnswer != proposed.Card.ExpectedAnswer
            || stored.Card.EvaluationMode != proposed.Card.EvaluationMode
            || stored.Card.CanProduceMasteryEvidence != proposed.Card.CanProduceMasteryEvidence
            || !stored.Card.Choices.SequenceEqual(proposed.Card.Choices)
            || stored.PolicyId != proposed.PolicyId
            || stored.PolicyVersion != proposed.PolicyVersion
            || stored.PolicyRevision != proposed.PolicyRevision)
        {
            throw new InvalidDataException("Une source de révision immuable a divergé.");
        }
    }

    private static void ValidateKeys(Guid profileId, Guid itemId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(itemId, Guid.Empty);
    }
}
