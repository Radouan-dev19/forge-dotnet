using System.Security.Cryptography;
using System.Text;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.Application.Mastery;

public sealed class MasteryService(
    ILocalProfileRepository profileRepository,
    IMasteryEvidenceSource evidenceSource,
    IMasteryProjectionRepository projectionRepository,
    IMasteryPolicySource policySource,
    TimeProvider timeProvider)
{
    public async ValueTask<MasteryDashboardView> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetAsync(cancellationToken);
        MasteryPolicy policy = policySource.Current;
        MasteryEvidenceSet evidence = await evidenceSource.ReadAsync(profile.LocalId, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        string calculationRevision = CalculateDailyRevision(evidence.Revision, policy.Revision, now);
        MasterySnapshot? snapshot = await projectionRepository.GetAsync(
            profile.LocalId,
            policy.Revision,
            calculationRevision,
            cancellationToken);
        if (snapshot is null)
        {
            var calculationEvidence = evidence with { Revision = calculationRevision };
            MasterySnapshot calculated = MasteryRules.Calculate(profile.LocalId, policy, calculationEvidence, now);
            snapshot = await projectionRepository.AppendAsync(policy, calculated, cancellationToken);
        }

        return Map(snapshot, evidence.Achievements);
    }

    /// <summary>
    /// Nature d'une preuve d'accomplissement, telle que la page l'affiche : vérifiée machine,
    /// attestée par un relecteur humain, ou déclarée — et une déclaration vaut zéro.
    /// </summary>
    private static MasteryAchievementView MapAchievement(MasteryAchievement achievement)
    {
        MasteryProofNature nature = achievement.Verification switch
        {
            MasteryVerificationKind.AutomaticTests
                or MasteryVerificationKind.ServerRubric
                or MasteryVerificationKind.ExamEngine
                or MasteryVerificationKind.ReviewEngine
                or MasteryVerificationKind.QuizEngine => MasteryProofNature.MachineVerified,
            MasteryVerificationKind.HumanAttestation => MasteryProofNature.HumanAttested,
            _ => MasteryProofNature.Declared,
        };
        bool counts = achievement.Passed && nature switch
        {
            MasteryProofNature.MachineVerified => true,
            MasteryProofNature.HumanAttested =>
                MasteryPolicyCatalog.HumanJudgementKeys.Contains(achievement.Key, StringComparer.Ordinal),
            _ => false,
        };
        return new MasteryAchievementView(
            achievement.Key,
            nature,
            nature switch
            {
                MasteryProofNature.MachineVerified => "vérifiée par la machine",
                MasteryProofNature.HumanAttested => "attestée par un relecteur humain, non vérifiée par la machine",
                _ => "déclarée — vaut zéro",
            },
            counts,
            achievement.ObservedAtUtc);
    }

    private static string CalculateDailyRevision(
        string evidenceRevision,
        string policyRevision,
        DateTimeOffset calculatedAtUtc)
    {
        string input = $"{evidenceRevision}|{policyRevision}|{calculatedAtUtc.UtcDateTime:yyyy-MM-dd}";
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)))}";
    }

    private static MasteryDashboardView Map(
        MasterySnapshot snapshot,
        IReadOnlyList<MasteryAchievement> achievements) => new(
        snapshot.PolicyId,
        snapshot.PolicyVersion,
        snapshot.PolicyRevision,
        snapshot.EvidenceRevision,
        snapshot.CalculatedAtUtc,
        snapshot.ObservationCount,
        Array.AsReadOnly(snapshot.Domains.Select(Map).ToArray()),
        Array.AsReadOnly(snapshot.Gates.Select(item => new MasteryGateView(
            item.Gate,
            item.Label,
            item.IsOpen,
            item.Blockers)).ToArray()),
        Array.AsReadOnly(achievements
            .Select(MapAchievement)
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ThenBy(item => item.ObservedAtUtc)
            .ToArray()));

    private static MasteryDomainView Map(MasteryDomainScore score) => new(
        score.Domain,
        DomainLabel(score.Domain),
        score.Score,
        score.RequiredScore,
        score.IsCritical,
        score.IsValidated,
        Array.AsReadOnly(score.Components.Select(item => new MasteryComponentView(
            item.Component,
            ComponentLabel(item.Component),
            item.Weight * 100m,
            item.Score,
            item.HasEvidence,
            item.EvidenceCount,
            item.DistinctItemCount)).ToArray()),
        score.Blockers);

    private static string DomainLabel(MasteryDomain domain) => domain switch
    {
        MasteryDomain.CSharp => "C#",
        MasteryDomain.Debugging => "Débogage",
        MasteryDomain.Sql => "SQL",
        MasteryDomain.Api => "API",
        MasteryDomain.Tests => "Tests",
        MasteryDomain.Docker => "Docker",
        MasteryDomain.ContinuousIntegration => "CI",
        MasteryDomain.Security => "Sécurité",
        MasteryDomain.Architecture => "Architecture",
        MasteryDomain.Performance => "Performance",
        MasteryDomain.English => "Anglais",
        _ => domain.ToString(),
    };

    private static string ComponentLabel(MasteryComponent component) => component switch
    {
        MasteryComponent.AutonomousPractice => "Pratique autonome",
        MasteryComponent.UnassistedExam => "Examen sans aide",
        MasteryComponent.SpacedRetention => "Rétention espacée",
        MasteryComponent.Explanation => "Explication",
        MasteryComponent.Quiz => "Quiz",
        _ => component.ToString(),
    };
}
