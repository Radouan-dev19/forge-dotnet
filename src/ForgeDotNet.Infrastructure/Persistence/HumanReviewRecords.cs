namespace ForgeDotNet.Infrastructure.Persistence;

/// <summary>
/// Attestation d'un relecteur humain, ajoutée sans jamais être modifiée : un constat daté ne se
/// réécrit pas. Le nom du relecteur est une déclaration libre et n'est jamais vérifiable par le
/// produit — c'est la nature assumée de ce canal, affichée partout où l'attestation apparaît.
/// </summary>
internal sealed class HumanAttestationRecord
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string TargetKey { get; set; } = string.Empty;

    public string ReviewerName { get; set; } = string.Empty;

    public string ReviewerRelation { get; set; } = string.Empty;

    public DateOnly ReviewedOn { get; set; }

    public int DurationMinutes { get; set; }

    public string ArtifactDescription { get; set; } = string.Empty;

    public string NamedGap { get; set; } = string.Empty;

    public string? ExplainedExerciseId { get; set; }

    public string CriteriaJson { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; }
}
