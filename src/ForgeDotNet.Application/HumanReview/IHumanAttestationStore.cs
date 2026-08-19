using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.HumanReview;

/// <summary>Une attestation enregistrée, telle que le produit la conserve et la restitue.</summary>
public sealed record HumanAttestationEntry(
    Guid Id,
    Guid ProfileId,
    string TargetKey,
    string ReviewerName,
    string ReviewerRelation,
    DateOnly ReviewedOn,
    int DurationMinutes,
    string ArtifactDescription,
    string NamedGap,
    string? ExplainedExerciseId,
    IReadOnlyList<HumanAttestationCriterionEntry> Criteria,
    DateTimeOffset RecordedAtUtc);

/// <summary>
/// Conservation des attestations humaines. Ajout seul, jamais de modification : une attestation
/// est un constat daté, et un constat ne se réécrit pas.
/// </summary>
public interface IHumanAttestationStore
{
    ValueTask<IReadOnlyList<HumanAttestationEntry>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsAsync(
        Guid profileId,
        string targetKey,
        DateOnly reviewedOn,
        CancellationToken cancellationToken = default);

    ValueTask AppendAsync(HumanAttestationEntry entry, CancellationToken cancellationToken = default);
}
