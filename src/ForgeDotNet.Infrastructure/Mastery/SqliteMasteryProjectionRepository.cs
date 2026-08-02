using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.Infrastructure.Mastery;

public sealed class SqliteMasteryProjectionRepository(
    IDbContextFactory<ForgeDbContext> contextFactory,
    LocalDatabaseGate databaseGate) : IMasteryProjectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public async ValueTask<MasterySnapshot?> GetAsync(
        Guid profileId,
        string policyRevision,
        string evidenceRevision,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(profileId, policyRevision, evidenceRevision);
        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        MasteryProjectionRecord? record = await context.MasteryProjections.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.ProfileId == profileId
                && item.PolicyRevision == policyRevision
                && item.EvidenceRevision == evidenceRevision,
                cancellationToken);
        return record is null ? null : Deserialize(record);
    }

    public async ValueTask<MasterySnapshot> AppendAsync(
        MasteryPolicy policy,
        MasterySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateKey(snapshot.ProfileId, snapshot.PolicyRevision, snapshot.EvidenceRevision);
        if (snapshot.PolicyId != policy.Id
            || snapshot.PolicyVersion != policy.Version
            || snapshot.PolicyRevision != policy.Revision)
        {
            throw new InvalidDataException("La projection ne correspond pas à la politique figée.");
        }

        await using var lease = await databaseGate.AcquireAsync(cancellationToken);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        MasteryProjectionRecord? existing = await FindAsync(context, snapshot, cancellationToken);
        if (existing is not null)
        {
            return Deserialize(existing);
        }

        context.MasteryProjections.Add(new MasteryProjectionRecord
        {
            Id = Guid.NewGuid(),
            ProfileId = snapshot.ProfileId,
            PolicyId = policy.Id,
            PolicyVersion = policy.Version,
            PolicyRevision = policy.Revision,
            EvidenceRevision = snapshot.EvidenceRevision,
            FrozenPolicyJson = JsonSerializer.Serialize(policy, JsonOptions),
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            CreatedAtUtc = snapshot.CalculatedAtUtc,
        });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return snapshot;
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            MasteryProjectionRecord? concurrent = await FindAsync(context, snapshot, cancellationToken);
            if (concurrent is null)
            {
                throw;
            }

            return Deserialize(concurrent);
        }
    }

    private static Task<MasteryProjectionRecord?> FindAsync(
        ForgeDbContext context,
        MasterySnapshot snapshot,
        CancellationToken cancellationToken) => context.MasteryProjections.AsNoTracking()
        .SingleOrDefaultAsync(item =>
            item.ProfileId == snapshot.ProfileId
            && item.PolicyRevision == snapshot.PolicyRevision
            && item.EvidenceRevision == snapshot.EvidenceRevision,
            cancellationToken);

    private static MasterySnapshot Deserialize(MasteryProjectionRecord record)
    {
        MasterySnapshot snapshot = JsonSerializer.Deserialize<MasterySnapshot>(record.SnapshotJson, JsonOptions)
            ?? throw new InvalidDataException("La projection de maîtrise persistée est illisible.");
        if (snapshot.ProfileId != record.ProfileId
            || snapshot.PolicyId != record.PolicyId
            || snapshot.PolicyVersion != record.PolicyVersion
            || snapshot.PolicyRevision != record.PolicyRevision
            || snapshot.EvidenceRevision != record.EvidenceRevision
            || string.IsNullOrWhiteSpace(record.FrozenPolicyJson))
        {
            throw new InvalidDataException("La projection de maîtrise persistée est incohérente.");
        }

        return snapshot;
    }

    private static void ValidateKey(Guid profileId, string policyRevision, string evidenceRevision)
    {
        if (profileId == Guid.Empty
            || string.IsNullOrWhiteSpace(policyRevision)
            || policyRevision.Length > 80
            || string.IsNullOrWhiteSpace(evidenceRevision)
            || evidenceRevision.Length > 80)
        {
            throw new ArgumentException("La clé de projection de maîtrise est invalide.");
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
