using System.Text.Json;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.UnitTests;

[Trait("Category", "MasteryAntiGaming")]
public sealed class MasteryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EasyQuizzesAloneCannotProduceMastery()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation[] quizzes = Enumerable.Range(1, 20)
            .Select(index => Observation(
                profileId,
                MasteryDomain.CSharp,
                MasteryComponent.Quiz,
                $"quiz-{index}",
                100m,
                MasteryAssistance.None,
                MasteryEvidenceSource.Quiz,
                MasteryVerificationKind.QuizEngine))
            .ToArray();

        MasteryDomainScore score = Domain(Calculate(profileId, quizzes), MasteryDomain.CSharp);

        Assert.Equal(5m, score.Score);
        Assert.False(score.IsValidated);
        Assert.Contains(score.Blockers, item => item.Contains("examen final", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ViewedSolutionZerosAutonomousPracticeForTheWholeItem()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation successful = Observation(
            profileId,
            MasteryDomain.CSharp,
            MasteryComponent.AutonomousPractice,
            "same-item",
            100m);
        MasteryObservation solution = Observation(
            profileId,
            MasteryDomain.CSharp,
            MasteryComponent.AutonomousPractice,
            "same-item",
            0m,
            MasteryAssistance.Solution,
            MasteryEvidenceSource.Practice,
            MasteryVerificationKind.ManualDeclaration);

        MasteryDomainScore score = Domain(Calculate(profileId, [successful, solution]), MasteryDomain.CSharp);

        Assert.Equal(0m, Component(score, MasteryComponent.AutonomousPractice).Score);
        Assert.False(score.IsValidated);
    }

    [Fact]
    public void SolutionContaminatedExercisesCannotSatisfyGateACount()
    {
        Guid profileId = Guid.NewGuid();
        var observations = new List<MasteryObservation>();
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.CSharp, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Debugging, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Sql, 100m));
        MasteryObservation[] practice = ExtraUnassistedPractice(profileId, 10);
        observations.AddRange(practice);
        observations.AddRange(practice.Select(item => item with
        {
            Id = Guid.NewGuid(),
            Score = 0m,
            Assistance = MasteryAssistance.Solution,
            Verification = MasteryVerificationKind.ManualDeclaration,
            EvidenceReference = $"solution:{item.Id:N}",
        }));

        MasteryGateResult gate = Gate(
            Calculate(profileId, observations, GateAAchievements(profileId)),
            MasteryGate.A);

        Assert.False(gate.IsOpen);
        Assert.Contains("10 exercices vérifiés sans aide", gate.Blockers);
    }

    [Theory]
    [InlineData(MasteryAssistance.Hint1, 90)]
    [InlineData(MasteryAssistance.Hint2, 80)]
    [InlineData(MasteryAssistance.Hint3, 70)]
    [InlineData(MasteryAssistance.Hint4, 60)]
    [InlineData(MasteryAssistance.Solution, 0)]
    public void AssistanceCapsCannotBeBypassed(MasteryAssistance assistance, decimal expected)
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation attempt = Observation(
            profileId,
            MasteryDomain.CSharp,
            MasteryComponent.AutonomousPractice,
            "hinted-item",
            100m,
            assistance);

        MasteryDomainScore score = Domain(Calculate(profileId, [attempt]), MasteryDomain.CSharp);

        Assert.Equal(expected, Component(score, MasteryComponent.AutonomousPractice).Score);
    }

    [Fact]
    public void RandomAndManualAttemptsDoNotCreateEvidence()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation[] attempts = Enumerable.Range(1, 50)
            .Select(index => Observation(
                profileId,
                MasteryDomain.CSharp,
                MasteryComponent.AutonomousPractice,
                $"random-{index}",
                100m,
                MasteryAssistance.None,
                MasteryEvidenceSource.Practice,
                MasteryVerificationKind.ManualDeclaration))
            .ToArray();

        MasteryDomainScore score = Domain(Calculate(profileId, attempts), MasteryDomain.CSharp);

        Assert.Equal(0m, score.Score);
        Assert.False(Component(score, MasteryComponent.AutonomousPractice).HasEvidence);
    }

    [Fact]
    public void RepeatingOneItemHasDiminishingWeightAndFailsVariety()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation[] attempts = Enumerable.Range(0, 12)
            .Select(index => Observation(
                profileId,
                MasteryDomain.CSharp,
                MasteryComponent.AutonomousPractice,
                "one-item",
                index == 0 ? 0m : 100m,
                observedAt: Now.AddMinutes(index - 12)))
            .ToArray();

        MasteryDomainScore score = Domain(Calculate(profileId, attempts), MasteryDomain.CSharp);

        Assert.Equal(1, score.DistinctItemCount);
        Assert.Contains(score.Blockers, item => item.Contains("Variété insuffisante", StringComparison.Ordinal));
        Assert.False(score.IsValidated);
    }

    [Fact]
    public void OldEvidenceCannotSatisfyRecency()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation[] observations = CompleteDomain(profileId, MasteryDomain.CSharp, 100m)
            .Select(item => item with { ObservedAtUtc = Now.AddDays(-91) })
            .ToArray();

        MasteryDomainScore score = Domain(Calculate(profileId, observations), MasteryDomain.CSharp);

        Assert.Equal(0m, score.Score);
        Assert.False(score.HasRecentUnassistedEvidence);
        Assert.False(score.IsValidated);
    }

    [Fact]
    public void RecentQuizCannotRefreshOldAutonomousEvidence()
    {
        Guid profileId = Guid.NewGuid();
        var observations = CompleteDomain(profileId, MasteryDomain.CSharp, 100m)
            .Select(item => item with { ObservedAtUtc = Now.AddDays(-31) })
            .ToList();
        observations.Add(Observation(
            profileId,
            MasteryDomain.CSharp,
            MasteryComponent.Quiz,
            "recent-quiz",
            100m,
            source: MasteryEvidenceSource.Quiz,
            verification: MasteryVerificationKind.QuizEngine));

        MasteryDomainScore score = Domain(Calculate(profileId, observations), MasteryDomain.CSharp);

        Assert.False(score.HasRecentUnassistedEvidence);
        Assert.False(score.IsValidated);
    }

    [Fact]
    public void CriticalDomainUsesEightyFiveEvenWhenOrdinaryThresholdIsEighty()
    {
        Guid profileId = Guid.NewGuid();
        MasteryDomainScore critical = Domain(
            Calculate(profileId, CompleteDomain(profileId, MasteryDomain.CSharp, 84m)),
            MasteryDomain.CSharp);
        MasteryDomainScore ordinary = Domain(
            Calculate(profileId, CompleteDomain(profileId, MasteryDomain.Security, 84m)),
            MasteryDomain.Security);

        Assert.False(critical.IsValidated);
        Assert.True(ordinary.IsValidated);
        Assert.Equal(85m, critical.RequiredScore);
        Assert.Equal(80m, ordinary.RequiredScore);
    }

    [Fact]
    public void HighDomainsCannotCompensateWeakSqlAtGateA()
    {
        Guid profileId = Guid.NewGuid();
        var observations = new List<MasteryObservation>();
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.CSharp, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Debugging, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Sql, 74m));
        observations.AddRange(ExtraUnassistedPractice(profileId, 10));
        MasteryAchievement[] achievements = GateAAchievements(profileId);

        MasteryGateResult gate = Gate(Calculate(profileId, observations, achievements), MasteryGate.A);

        Assert.False(gate.IsOpen);
        Assert.Contains("SQL ≥ 75", gate.Blockers);
    }

    [Fact]
    public void MissingComponentContributesZeroInsteadOfBeingImputed()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation[] withoutQuiz = CompleteDomain(profileId, MasteryDomain.CSharp, 100m)
            .Where(item => item.Component != MasteryComponent.Quiz)
            .ToArray();

        MasteryDomainScore score = Domain(Calculate(profileId, withoutQuiz), MasteryDomain.CSharp);

        Assert.Equal(95m, score.Score);
        MasteryComponentScore quiz = Component(score, MasteryComponent.Quiz);
        Assert.False(quiz.HasEvidence);
        Assert.Equal(0m, quiz.Score);
    }

    [Fact]
    public void FakeExamCannotSatisfyExamComponent()
    {
        Guid profileId = Guid.NewGuid();
        var observations = CompleteDomain(profileId, MasteryDomain.CSharp, 100m)
            .Where(item => item.Component != MasteryComponent.UnassistedExam)
            .ToList();
        observations.Add(Observation(
            profileId,
            MasteryDomain.CSharp,
            MasteryComponent.UnassistedExam,
            "declared-exam",
            100m,
            MasteryAssistance.None,
            MasteryEvidenceSource.Practice,
            MasteryVerificationKind.ManualDeclaration));

        MasteryDomainScore score = Domain(Calculate(profileId, observations), MasteryDomain.CSharp);

        Assert.False(score.HasUnassistedExam);
        Assert.False(Component(score, MasteryComponent.UnassistedExam).HasEvidence);
        Assert.False(score.IsValidated);
    }

    [Fact]
    public void GatesRemainClosedWithoutEveryDeliverableAndPrerequisite()
    {
        Guid profileId = Guid.NewGuid();
        var observations = new List<MasteryObservation>();
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.CSharp, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Debugging, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Sql, 100m));
        observations.AddRange(ExtraUnassistedPractice(profileId, 10));

        MasterySnapshot snapshot = Calculate(profileId, observations);

        Assert.All(snapshot.Gates, gate => Assert.False(gate.IsOpen));
        Assert.Contains(MasteryPolicyCatalog.ConsoleProject, GateARequirementKeys());
        Assert.Contains(Gate(snapshot, MasteryGate.B).Blockers, item => item.Contains("Porte A", StringComparison.Ordinal));
    }

    /// <summary>
    /// La porte A ne tenait plus qu'à un chaînon : le projet console, qu'aucun producteur n'émettait.
    /// </summary>
    /// <remarks>
    /// Ce cas isole ce blocage. Un profil complet sur tout le reste, examen de quatre-vingt-dix
    /// minutes compris, reste bloqué au seul motif « Mini-projet console vérifié ». La même
    /// projection avec l'accomplissement produit par une soumission vérifiée ouvre la porte — et une
    /// déclaration manuelle, elle, ne l'ouvre pas.
    /// </remarks>
    [Fact]
    public void GateAHangsOnTheConsoleProjectAndOpensOnlyOnAVerifiedOne()
    {
        Guid profileId = Guid.NewGuid();
        var observations = new List<MasteryObservation>();
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.CSharp, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Debugging, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Sql, 100m));
        observations.AddRange(ExtraUnassistedPractice(profileId, 10));
        MasteryAchievement[] examOnly =
        [
            Achievement(profileId, MasteryPolicyCatalog.NinetyMinuteExam, 90, MasteryVerificationKind.ExamEngine),
        ];

        MasteryGateResult blocked = Gate(Calculate(profileId, observations, examOnly), MasteryGate.A);

        Assert.False(blocked.IsOpen);
        Assert.Equal("Mini-projet console vérifié", Assert.Single(blocked.Blockers));

        MasteryGateResult declared = Gate(
            Calculate(
                profileId,
                observations,
                [
                    .. examOnly,
                    Achievement(
                        profileId,
                        MasteryPolicyCatalog.ConsoleProject,
                        verification: MasteryVerificationKind.ManualDeclaration),
                ]),
            MasteryGate.A);

        Assert.False(declared.IsOpen);
        Assert.Equal("Mini-projet console vérifié", Assert.Single(declared.Blockers));

        MasteryGateResult verified = Gate(
            Calculate(
                profileId,
                observations,
                [
                    .. examOnly,
                    Achievement(
                        profileId,
                        MasteryPolicyCatalog.ConsoleProject,
                        verification: MasteryVerificationKind.AutomaticTests),
                ]),
            MasteryGate.A);

        Assert.True(verified.IsOpen);
        Assert.Empty(verified.Blockers);
    }

    [Fact]
    public void ExactDeliverablesCanOpenAllFourGatesInOrder()
    {
        Guid profileId = Guid.NewGuid();
        var observations = new List<MasteryObservation>();
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.CSharp, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Debugging, 100m));
        observations.AddRange(CompleteDomain(profileId, MasteryDomain.Sql, 100m));
        observations.AddRange(ExtraUnassistedPractice(profileId, 10));
        MasteryAchievement[] achievements = AllAchievements(profileId);

        MasterySnapshot open = Calculate(profileId, observations, achievements);
        Assert.All(open.Gates, gate => Assert.True(gate.IsOpen));

        MasterySnapshot missingDeployment = Calculate(
            profileId,
            observations,
            achievements.Where(item => item.Key != MasteryPolicyCatalog.Deployment).ToArray());
        Assert.False(Gate(missingDeployment, MasteryGate.C).IsOpen);
        Assert.False(Gate(missingDeployment, MasteryGate.D).IsOpen);
    }

    [Fact]
    public void BoundsRoundingReplayIdempotenceAndPolicyVersionAreStable()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation observation = Observation(
            profileId,
            MasteryDomain.Security,
            MasteryComponent.AutonomousPractice,
            "rounding",
            33.335m);
        MasterySnapshot first = Calculate(profileId, [observation]);
        MasterySnapshot second = Calculate(profileId, [observation]);

        Assert.Equal(15m, Domain(first, MasteryDomain.Security).Score);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(1, first.PolicyVersion);
        Assert.Equal(MasteryPolicyCatalog.Version1.Revision, first.PolicyRevision);

        MasteryObservation invalid = observation with { Id = Guid.NewGuid(), Score = 100.01m };
        Assert.Throws<InvalidOperationException>(() => Calculate(profileId, [invalid]));
        Assert.Throws<InvalidOperationException>(() => Calculate(profileId, [observation, observation]));
    }

    /// <summary>
    /// Documente le défaut : sans preuve de rétention, un travail excellent reste sous le seuil.
    /// </summary>
    /// <remarks>
    /// Le poids de la composante absente n'est jamais redistribué. Un domaine critique exige 85 ;
    /// le maximum atteignable sans rétention est exactement 85, ce qui suppose 100 partout ailleurs.
    /// Un profil réaliste — pratique 95, examen 90, explication 90, quiz 100 — plafonne à 79,25.
    /// </remarks>
    [Fact]
    public void WithoutSpacedRetentionEvidenceAStrongDomainStaysBelowTheCriticalThreshold()
    {
        Guid profileId = Guid.NewGuid();

        MasteryDomainScore score = Domain(
            Calculate(profileId, RealisticDomain(profileId, MasteryDomain.Api, withRetention: false)),
            MasteryDomain.Api);

        Assert.Equal(0m, Component(score, MasteryComponent.SpacedRetention).Score);
        Assert.Equal(79.25m, score.Score);
        Assert.False(score.IsValidated);
    }

    /// <summary>
    /// Prouve la correction : une preuve de rétention vérifiée débloque le même profil, sans qu'aucun
    /// seuil n'ait été abaissé.
    /// </summary>
    [Fact]
    public void SpacedRetentionEvidenceLetsTheSameProfileReachTheCriticalThreshold()
    {
        Guid profileId = Guid.NewGuid();

        MasteryDomainScore score = Domain(
            Calculate(profileId, RealisticDomain(profileId, MasteryDomain.Api, withRetention: true)),
            MasteryDomain.Api);

        Assert.Equal(100m, Component(score, MasteryComponent.SpacedRetention).Score);
        Assert.Equal(94.25m, score.Score);
        Assert.True(score.IsValidated);
    }

    /// <summary>
    /// Borne l'élargissement : une réponse auto-évaluée n'alimente toujours pas la rétention.
    /// </summary>
    [Fact]
    public void SelfAssessedReviewAnswersRemainIneligibleForSpacedRetention()
    {
        Guid profileId = Guid.NewGuid();
        MasteryObservation[] observations =
        [
            .. RealisticDomain(profileId, MasteryDomain.Api, withRetention: false),
            Observation(
                profileId,
                MasteryDomain.Api,
                MasteryComponent.SpacedRetention,
                "api-self-review",
                100m,
                source: MasteryEvidenceSource.Review,
                verification: MasteryVerificationKind.ManualDeclaration),
        ];

        MasteryDomainScore score = Domain(Calculate(profileId, observations), MasteryDomain.Api);

        Assert.Equal(0m, Component(score, MasteryComponent.SpacedRetention).Score);
        Assert.Equal(79.25m, score.Score);
    }

    /// <summary>
    /// Profil de travail sérieux mais imparfait, sur un domaine critique : trois pratiques sans aide
    /// à 95, un examen à 90, une explication à 90 et un quiz à 100.
    /// </summary>
    private static MasteryObservation[] RealisticDomain(
        Guid profileId,
        MasteryDomain domain,
        bool withRetention)
    {
        var observations = new List<MasteryObservation>
        {
            Observation(profileId, domain, MasteryComponent.AutonomousPractice, $"{domain}-practice-1", 95m),
            Observation(profileId, domain, MasteryComponent.AutonomousPractice, $"{domain}-practice-2", 95m),
            Observation(profileId, domain, MasteryComponent.AutonomousPractice, $"{domain}-practice-3", 95m),
            Observation(profileId, domain, MasteryComponent.UnassistedExam, $"{domain}-exam", 90m,
                source: MasteryEvidenceSource.Exam, verification: MasteryVerificationKind.ExamEngine),
            Observation(profileId, domain, MasteryComponent.Explanation, $"{domain}-explanation", 90m,
                source: MasteryEvidenceSource.Explanation, verification: MasteryVerificationKind.ServerRubric),
            Observation(profileId, domain, MasteryComponent.Quiz, $"{domain}-quiz", 100m,
                source: MasteryEvidenceSource.Quiz, verification: MasteryVerificationKind.QuizEngine),
        };

        if (withRetention)
        {
            observations.Add(Observation(
                profileId,
                domain,
                MasteryComponent.SpacedRetention,
                $"{domain}-review-card",
                100m,
                source: MasteryEvidenceSource.Review,
                verification: MasteryVerificationKind.ReviewEngine));
        }

        return observations.ToArray();
    }

    private static MasterySnapshot Calculate(
        Guid profileId,
        IEnumerable<MasteryObservation> observations,
        IEnumerable<MasteryAchievement>? achievements = null) => MasteryRules.Calculate(
            profileId,
            MasteryPolicyCatalog.Version1,
            new MasteryEvidenceSet(
                Array.AsReadOnly(observations.ToArray()),
                Array.AsReadOnly((achievements ?? []).ToArray()),
                "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Now);

    private static MasteryObservation Observation(
        Guid profileId,
        MasteryDomain domain,
        MasteryComponent component,
        string itemId,
        decimal score,
        MasteryAssistance assistance = MasteryAssistance.None,
        MasteryEvidenceSource source = MasteryEvidenceSource.Practice,
        MasteryVerificationKind verification = MasteryVerificationKind.AutomaticTests,
        DateTimeOffset? observedAt = null) => new(
            Guid.NewGuid(),
            profileId,
            domain,
            component,
            source,
            verification,
            itemId,
            1,
            "sha256:content",
            score,
            assistance,
            observedAt ?? Now.AddDays(-1),
            $"evidence:{Guid.NewGuid():N}");

    private static IReadOnlyList<MasteryObservation> CompleteDomain(
        Guid profileId,
        MasteryDomain domain,
        decimal score) =>
    [
        Observation(profileId, domain, MasteryComponent.AutonomousPractice, $"{domain}-practice-1", score),
        Observation(profileId, domain, MasteryComponent.AutonomousPractice, $"{domain}-practice-2", score),
        Observation(profileId, domain, MasteryComponent.AutonomousPractice, $"{domain}-practice-3", score),
        Observation(profileId, domain, MasteryComponent.UnassistedExam, $"{domain}-exam", score,
            source: MasteryEvidenceSource.Exam, verification: MasteryVerificationKind.ExamEngine),
        Observation(profileId, domain, MasteryComponent.SpacedRetention, $"{domain}-review", score,
            source: MasteryEvidenceSource.Review, verification: MasteryVerificationKind.ReviewEngine),
        Observation(profileId, domain, MasteryComponent.Explanation, $"{domain}-explanation", score,
            source: MasteryEvidenceSource.Explanation, verification: MasteryVerificationKind.ServerRubric),
        Observation(profileId, domain, MasteryComponent.Quiz, $"{domain}-quiz", score,
            source: MasteryEvidenceSource.Quiz, verification: MasteryVerificationKind.QuizEngine),
    ];

    private static MasteryObservation[] ExtraUnassistedPractice(Guid profileId, int count) =>
        Enumerable.Range(1, count)
            .Select(index => Observation(
                profileId,
                MasteryDomain.CSharp,
                MasteryComponent.AutonomousPractice,
                $"gate-exercise-{index}",
                100m))
            .ToArray();

    private static MasteryAchievement[] GateAAchievements(Guid profileId) =>
    [
        Achievement(profileId, MasteryPolicyCatalog.ConsoleProject),
        Achievement(profileId, MasteryPolicyCatalog.NinetyMinuteExam, 90, MasteryVerificationKind.ExamEngine),
    ];

    private static MasteryAchievement[] AllAchievements(Guid profileId)
    {
        string[] keys =
        [
            MasteryPolicyCatalog.ConsoleProject,
            MasteryPolicyCatalog.NinetyMinuteExam,
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
        return keys.Select(key => Achievement(
            profileId,
            key,
            key == MasteryPolicyCatalog.NinetyMinuteExam ? 90
                : key == MasteryPolicyCatalog.TenMinutePresentation ? 10 : 0,
            key == MasteryPolicyCatalog.NinetyMinuteExam
                ? MasteryVerificationKind.ExamEngine
                : MasteryVerificationKind.ServerRubric)).ToArray();
    }

    private static MasteryAchievement Achievement(
        Guid profileId,
        string key,
        int duration = 0,
        MasteryVerificationKind verification = MasteryVerificationKind.ServerRubric) => new(
            Guid.NewGuid(),
            profileId,
            key,
            verification,
            true,
            duration,
            Now.AddDays(-1),
            $"achievement:{key}");

    private static MasteryDomainScore Domain(MasterySnapshot snapshot, MasteryDomain domain) =>
        snapshot.Domains.Single(item => item.Domain == domain);

    private static MasteryComponentScore Component(MasteryDomainScore score, MasteryComponent component) =>
        score.Components.Single(item => item.Component == component);

    private static MasteryGateResult Gate(MasterySnapshot snapshot, MasteryGate gate) =>
        snapshot.Gates.Single(item => item.Gate == gate);

    private static IEnumerable<string> GateARequirementKeys() => MasteryPolicyCatalog.Version1.Gates
        .Single(item => item.Gate == MasteryGate.A)
        .Requirements
        .Where(item => item.AchievementKey is not null)
        .Select(item => item.AchievementKey!);
}
