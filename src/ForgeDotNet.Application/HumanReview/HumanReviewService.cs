using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.HumanReview;

/// <summary>Ce que l'apprenant saisit au retour d'une séance de revue.</summary>
public sealed record RecordHumanAttestationCommand(
    string TargetKey,
    string ReviewerName,
    string ReviewerRelation,
    DateOnly ReviewedOn,
    int DurationMinutes,
    string ArtifactDescription,
    string NamedGap,
    string? ExplainedExerciseId,
    IReadOnlyList<HumanAttestationCriterionEntry> Criteria);

public sealed record RecordHumanAttestationResult(bool Succeeded, IReadOnlyList<string> Failures);

/// <summary>L'état d'une exigence du protocole : sa grille, et les attestations enregistrées.</summary>
public sealed record HumanReviewRequirementView(
    HumanReviewGrid Grid,
    IReadOnlyList<HumanAttestationEntry> Attestations);

public sealed record HumanReviewOverview(
    string LearnerDisplayName,
    IReadOnlyList<HumanReviewRequirementView> Requirements);

/// <summary>
/// Le canal produit du protocole de revue par un tiers. Il enregistre une attestation complète,
/// signée d'un relecteur qui n'est pas l'apprenant, sur une grille du protocole — et rien d'autre.
/// Une attestation n'est jamais une preuve machine : la projection de maîtrise la porte sous le
/// type <see cref="MasteryVerificationKind.HumanAttestation"/>, admis exclusivement pour les
/// exigences à jugement humain.
/// </summary>
public sealed class HumanReviewService(
    ILocalProfileRepository profileRepository,
    IHumanAttestationStore store,
    TimeProvider timeProvider)
{
    public async ValueTask<HumanReviewOverview> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetAsync(cancellationToken);
        IReadOnlyList<HumanAttestationEntry> attestations = await store.ListAsync(profile.LocalId, cancellationToken);
        HumanReviewRequirementView[] requirements = HumanReviewCatalog.Grids
            .Select(grid => new HumanReviewRequirementView(
                grid,
                Array.AsReadOnly(attestations
                    .Where(entry => string.Equals(entry.TargetKey, grid.TargetKey, StringComparison.Ordinal))
                    .OrderBy(entry => entry.ReviewedOn)
                    .ToArray())))
            .ToArray();
        return new HumanReviewOverview(profile.DisplayName, Array.AsReadOnly(requirements));
    }

    public async ValueTask<RecordHumanAttestationResult> RecordAsync(
        RecordHumanAttestationCommand command,
        CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetAsync(cancellationToken);
        var draft = new HumanAttestationDraft(
            command.TargetKey,
            command.ReviewerName,
            command.ReviewerRelation,
            profile.DisplayName,
            command.ReviewedOn,
            command.DurationMinutes,
            command.ArtifactDescription,
            command.NamedGap,
            command.ExplainedExerciseId,
            command.Criteria);
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(draft);
        if (failures.Count > 0)
        {
            return new RecordHumanAttestationResult(false, failures);
        }

        // Le rejeu n'ajoute rien : la même revue, du même jour, sur la même exigence, existe déjà.
        bool duplicated = await store.ExistsAsync(
            profile.LocalId, command.TargetKey, command.ReviewedOn, cancellationToken);
        if (duplicated)
        {
            return new RecordHumanAttestationResult(
                false,
                ["Une attestation de cette exigence à cette date est déjà enregistrée : le rejeu d'une même revue ne produit rien de plus."]);
        }

        await store.AppendAsync(
            new HumanAttestationEntry(
                Guid.NewGuid(),
                profile.LocalId,
                command.TargetKey,
                command.ReviewerName.Trim(),
                command.ReviewerRelation.Trim(),
                command.ReviewedOn,
                command.DurationMinutes,
                command.ArtifactDescription.Trim(),
                command.NamedGap.Trim(),
                string.IsNullOrWhiteSpace(command.ExplainedExerciseId) ? null : command.ExplainedExerciseId.Trim(),
                command.Criteria,
                timeProvider.GetUtcNow()),
            cancellationToken);
        return new RecordHumanAttestationResult(true, []);
    }
}
