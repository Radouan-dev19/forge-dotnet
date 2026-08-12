using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Projects;

namespace ForgeDotNet.UnitTests;

/// <summary>
/// Rend visible ce qu'une porte exige et ce que le produit sait réellement produire.
/// </summary>
/// <remarks>
/// Le défaut d'origine n'était pas une règle fausse : c'était un chaînon absent, et surtout
/// invisible. <c>MasteryPolicyCatalog</c> déclarait vingt-trois clés d'accomplissement ; une seule
/// avait un producteur. Les quatre portes étaient donc fermées définitivement, sans que rien dans le
/// code ne le signale.
///
/// Ce test rend l'inventaire obligatoire : toute clé exigée par une porte doit figurer soit parmi
/// les clés produites, soit parmi les clés déclarées sans producteur. On ne peut plus ajouter une
/// exigence sans dire qui la satisfait, ni oublier qu'une exigence n'est satisfaite par personne.
/// </remarks>
public sealed class ProjectAchievementTests
{
    /// <summary>Clés qu'un producteur du produit sait réellement émettre.</summary>
    private static readonly string[] ProducedAchievementKeys =
    [
        // SqliteMasteryEvidenceSource.AddExamsAsync
        MasteryPolicyCatalog.NinetyMinuteExam,

        // SqliteMasteryEvidenceSource.AddProjectsAsync, sur déclaration du contenu
        MasteryPolicyCatalog.ConsoleProject,
    ];

    /// <summary>
    /// Clés qu'aucun producteur n'émet à ce jour. Ce nombre ne peut que descendre.
    /// </summary>
    private static readonly string[] UnproducedAchievementKeys =
    [
        MasteryPolicyCatalog.ApiFunctional,
        MasteryPolicyCatalog.EfCore,
        MasteryPolicyCatalog.ValidationAndErrors,
        MasteryPolicyCatalog.UnitTests,
        MasteryPolicyCatalog.IntegrationTests,
        MasteryPolicyCatalog.CleanGit,
        MasteryPolicyCatalog.TenMinutePresentation,
        MasteryPolicyCatalog.Docker,
        MasteryPolicyCatalog.ContinuousIntegration,
        MasteryPolicyCatalog.AuthenticationAuthorization,
        MasteryPolicyCatalog.Logs,
        MasteryPolicyCatalog.Deployment,
        MasteryPolicyCatalog.SimulatedIncident,
        MasteryPolicyCatalog.MockInterview,
        MasteryPolicyCatalog.Performance,
        MasteryPolicyCatalog.Security,
        MasteryPolicyCatalog.PragmaticArchitecture,
        MasteryPolicyCatalog.AutonomousFeature,
        MasteryPolicyCatalog.CodeReview,
        MasteryPolicyCatalog.English,
        MasteryPolicyCatalog.FinalDefense,
    ];

    /// <summary>Plafond de clés sans producteur. Il ne peut que descendre.</summary>
    private const int MaximumUnproducedKeys = 21;

    [Fact]
    public void EveryGateRequirementIsEitherProducedOrDeclaredUnproduced()
    {
        string[] required = MasteryPolicyCatalog.Version1.Gates
            .SelectMany(gate => gate.Requirements)
            .Where(requirement => requirement.AchievementKey is not null)
            .Select(requirement => requirement.AchievementKey!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        string[] inventoried = [.. ProducedAchievementKeys, .. UnproducedAchievementKeys];
        string[] undeclared = required.Except(inventoried, StringComparer.Ordinal).ToArray();
        string[] orphans = inventoried.Except(required, StringComparer.Ordinal).ToArray();

        Assert.True(
            undeclared.Length == 0,
            "Ces exigences de porte ne sont ni produites ni déclarées sans producteur : "
            + string.Join(", ", undeclared));
        Assert.True(
            orphans.Length == 0,
            "Ces clés inventoriées ne sont exigées par aucune porte : " + string.Join(", ", orphans));
        Assert.Equal(
            inventoried.Length,
            inventoried.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheNumberOfRequirementsWithoutAProducerNeverGrows()
    {
        Assert.True(
            UnproducedAchievementKeys.Length <= MaximumUnproducedKeys,
            $"{UnproducedAchievementKeys.Length} exigences sans producteur, pour un plafond de "
            + $"{MaximumUnproducedKeys}.");
    }

    /// <summary>
    /// La porte A dépend d'une clé désormais produite : elle est franchissable par le travail.
    /// </summary>
    [Fact]
    public void GateARequiresOnlyKeysThatHaveAProducer()
    {
        string[] gateAKeys = MasteryPolicyCatalog.Version1.Gates
            .Single(gate => gate.Gate == MasteryGate.A)
            .Requirements
            .Where(requirement => requirement.AchievementKey is not null)
            .Select(requirement => requirement.AchievementKey!)
            .ToArray();

        Assert.All(gateAKeys, key => Assert.Contains(key, ProducedAchievementKeys, StringComparer.Ordinal));
    }

    /// <summary>
    /// Une soumission ne peut pas se déclarer réussie sans avoir été exécutée.
    /// </summary>
    /// <remarks>
    /// C'est la garantie qui autorise le producteur à en tirer un accomplissement : le refus vit
    /// dans le domaine, pas dans l'interface, et aucun appelant ne peut le contourner.
    /// </remarks>
    [Theory]
    [InlineData(false, 3, 3, 12, 12, "non exécutée")]
    [InlineData(true, 3, 2, 12, 8, "une suite en échec")]
    [InlineData(true, 0, 0, 0, 0, "aucune suite")]
    [InlineData(true, 3, 3, 0, 0, "aucun test rapporté")]
    public void ASuccessfulSubmissionMustBeProvenByEverySuite(
        bool verified,
        int totalSuites,
        int passedSuites,
        int totalTests,
        int passedTests,
        string reason)
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Succeeded, verified, totalSuites, passedSuites, totalTests, passedTests);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(submission.Validate);

        Assert.Contains("prouvée", error.Message, StringComparison.Ordinal);
        Assert.False(submission.ProducesEvidence, reason);
    }

    [Fact]
    public void AManualDeclarationNeverClaimsToBeAutomaticallyVerified()
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Declared, verified: true, 3, 3, 12, 12);

        Assert.Throws<InvalidOperationException>(submission.Validate);
    }

    [Fact]
    public void AManualSubmissionIsRecordedButProducesNoEvidence()
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Declared, verified: false, 3, 0, 0, 0);

        submission.Validate();

        Assert.False(submission.ProducesEvidence);
    }

    [Fact]
    public void AFullyVerifiedSubmissionProducesEvidence()
    {
        ProjectSubmission submission = Submission(
            ProjectSubmissionStatus.Succeeded, verified: true, 3, 3, 12, 12);

        submission.Validate();

        Assert.True(submission.ProducesEvidence);
    }

    /// <summary>
    /// Un projet sans clé déclarée ne prouve rien, même vérifié de bout en bout.
    /// </summary>
    [Fact]
    public void AVerifiableProjectWithoutADeclaredKeyProducesNoAchievement()
    {
        Project withoutKey = ProjectWith(achievementKey: null);
        Project withKey = ProjectWith(MasteryPolicyCatalog.ConsoleProject);

        Assert.True(withoutKey.IsVerifiable);
        Assert.False(withoutKey.ProducesAchievement);
        Assert.True(withKey.ProducesAchievement);
    }

    private static ProjectSubmission Submission(
        ProjectSubmissionStatus status,
        bool verified,
        int totalSuites,
        int passedSuites,
        int totalTests,
        int passedTests) => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "project-collections-library-001",
            2,
            "sha256:" + new string('a', 64),
            "sha256:" + new string('b', 64),
            status,
            totalSuites,
            passedSuites,
            totalTests,
            passedTests,
            verified,
            DateTimeOffset.UnixEpoch.AddDays(1));

    private static Project ProjectWith(string? achievementKey) => new(
        "project-collections-library-001",
        2,
        "Bibliothèque de collections",
        2,
        [2],
        ["csharp.collections"],
        8,
        "brief",
        [new ProjectStarterFile("Submission.cs", "public static class Submission { }")],
        4,
        [new ProjectMilestone("normalisation", "Assainir", "preuve", ["critère"])],
        [new ProjectRubricCriterion("critère", 1m, "preuve observable")],
        [new ProjectAcceptanceSuite("normalisation", "normalisation/")],
        ["erreur fréquente"],
        achievementKey,
        "sha256:" + new string('a', 64));
}
