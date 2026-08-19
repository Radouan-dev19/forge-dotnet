using System.Text.Json;
using ForgeDotNet.Application.HumanReview;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.HumanReview;

public sealed class SqliteHumanAttestationStore(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IHumanAttestationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<IReadOnlyList<HumanAttestationEntry>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        HumanAttestationRecord[] records = await context.HumanAttestations.AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .OrderBy(item => item.RecordedAtUtc)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(records.Select(Map).ToArray());
    }

    public async ValueTask<bool> ExistsAsync(
        Guid profileId,
        string targetKey,
        DateOnly reviewedOn,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HumanAttestations.AsNoTracking().AnyAsync(
            item => item.ProfileId == profileId
                && item.TargetKey == targetKey
                && item.ReviewedOn == reviewedOn,
            cancellationToken);
    }

    public async ValueTask AppendAsync(
        HumanAttestationEntry entry,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.HumanAttestations.Add(new HumanAttestationRecord
        {
            Id = entry.Id,
            ProfileId = entry.ProfileId,
            TargetKey = entry.TargetKey,
            ReviewerName = entry.ReviewerName,
            ReviewerRelation = entry.ReviewerRelation,
            ReviewedOn = entry.ReviewedOn,
            DurationMinutes = entry.DurationMinutes,
            ArtifactDescription = entry.ArtifactDescription,
            NamedGap = entry.NamedGap,
            ExplainedExerciseId = entry.ExplainedExerciseId,
            CriteriaJson = JsonSerializer.Serialize(entry.Criteria, JsonOptions),
            RecordedAtUtc = entry.RecordedAtUtc,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static HumanAttestationEntry Map(HumanAttestationRecord record) => new(
        record.Id,
        record.ProfileId,
        record.TargetKey,
        record.ReviewerName,
        record.ReviewerRelation,
        record.ReviewedOn,
        record.DurationMinutes,
        record.ArtifactDescription,
        record.NamedGap,
        record.ExplainedExerciseId,
        Array.AsReadOnly(
            JsonSerializer.Deserialize<HumanAttestationCriterionEntry[]>(record.CriteriaJson, JsonOptions) ?? []),
        record.RecordedAtUtc);
}
