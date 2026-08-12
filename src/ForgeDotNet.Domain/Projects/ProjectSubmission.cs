namespace ForgeDotNet.Domain.Projects;

public enum ProjectSubmissionStatus
{
    Succeeded,
    CompilationFailed,
    TestsFailed,
    TimedOut,
    Cancelled,
    Unavailable,
    Declared,
}

/// <summary>
/// Soumission de projet, ajoutée sans jamais être modifiée.
/// </summary>
/// <remarks>
/// Une soumission porte l'ensemble des suites d'acceptation du projet, et non une seule : un projet
/// n'est livré que lorsque tous ses jalons tiennent ensemble. <see cref="AutomaticallyVerified"/>
/// distingue une exécution réelle dans le bac à sable d'une déclaration faite en mode manuel — et
/// <see cref="Validate"/> refuse une réussite qui ne serait pas prouvée par des suites exécutées.
/// C'est cette garantie qui autorise la projection de maîtrise à en tirer un accomplissement.
/// </remarks>
public sealed record ProjectSubmission(
    Guid Id,
    Guid ProfileId,
    string ProjectId,
    int ProjectVersion,
    string ContentRevision,
    string SubmissionFingerprint,
    ProjectSubmissionStatus Status,
    int TotalSuites,
    int PassedSuites,
    int TotalTests,
    int PassedTests,
    bool AutomaticallyVerified,
    DateTimeOffset ObservedAtUtc)
{
    public void Validate()
    {
        if (Id == Guid.Empty
            || ProfileId == Guid.Empty
            || string.IsNullOrWhiteSpace(ProjectId)
            || ProjectId.Length > 100
            || ProjectVersion < 1
            || string.IsNullOrWhiteSpace(ContentRevision)
            || ContentRevision.Length > 80
            || !IsSha256(SubmissionFingerprint)
            || TotalSuites < 0
            || PassedSuites < 0
            || PassedSuites > TotalSuites
            || TotalTests < 0
            || PassedTests < 0
            || PassedTests > TotalTests)
        {
            throw new InvalidOperationException("La soumission de projet est invalide.");
        }

        if (Status == ProjectSubmissionStatus.Declared && AutomaticallyVerified)
        {
            throw new InvalidOperationException(
                "Une déclaration manuelle ne peut pas se présenter comme vérifiée automatiquement.");
        }

        if (Status == ProjectSubmissionStatus.Succeeded
            && (!AutomaticallyVerified
                || TotalSuites == 0
                || PassedSuites != TotalSuites
                || TotalTests == 0
                || PassedTests != TotalTests))
        {
            throw new InvalidOperationException(
                "Une réussite de projet doit être prouvée par toutes ses suites, réellement exécutées.");
        }
    }

    /// <summary>
    /// Vrai lorsque la soumission peut alimenter un accomplissement de maîtrise.
    /// </summary>
    /// <remarks>
    /// Les conditions sont exactement celles qui rendent une réussite valide : les deux ne doivent
    /// jamais diverger, sans quoi une soumission refusée par <see cref="Validate"/> pourrait
    /// néanmoins se présenter comme une preuve.
    /// </remarks>
    public bool ProducesEvidence =>
        Status == ProjectSubmissionStatus.Succeeded
        && AutomaticallyVerified
        && TotalSuites > 0
        && PassedSuites == TotalSuites
        && TotalTests > 0
        && PassedTests == TotalTests;

    private static bool IsSha256(string? value) => value is not null
        && value.Length == 71
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(char.IsAsciiHexDigit);
}
