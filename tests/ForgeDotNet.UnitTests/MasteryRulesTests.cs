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

    /// <summary>
    /// Aucune composante n'admet une déclaration manuelle, quelle qu'elle soit.
    /// </summary>
    /// <remarks>
    /// C'est la garantie qui rend publiable le protocole de revue par un tiers
    /// (<c>docs/HUMAN_REVIEW.md</c>). Six exigences de porte et la composante Explication ne peuvent
    /// être attestées que par un humain ; le protocole décrit comment, et consigne le verdict dans un
    /// fichier que l'apprenant conserve, hors du produit. Rien n'empêcherait un incrément futur de
    /// vouloir « remonter » ces attestations dans la base — sauf ce test, qui vérifie composante par
    /// composante qu'une déclaration ne pèse rien. Le protocole peut donc exister sans qu'aucun faux
    /// signal ne devienne possible par simple ajout de code.
    /// </remarks>
    [Fact]
    public void AManualDeclarationNeverFeedsAnyComponent()
    {
        var offenders = new List<string>();

        foreach (MasteryComponent component in Enum.GetValues<MasteryComponent>())
        {
            Guid profileId = Guid.NewGuid();
            MasterySnapshot snapshot = Calculate(
                profileId,
                [
                    Observation(
                        profileId,
                        MasteryDomain.CSharp,
                        component,
                        $"declared-{component}",
                        100m,
                        verification: MasteryVerificationKind.ManualDeclaration),
                ]);

            MasteryComponentScore score = snapshot.Domains
                .Single(domain => domain.Domain == MasteryDomain.CSharp)
                .Components
                .Single(item => item.Component == component);

            if (score.HasEvidence || score.Score != 0m)
            {
                offenders.Add($"{component} : une déclaration manuelle y compte pour {score.Score}.");
            }
        }

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    /// <summary>
    /// Une exigence de jugement humain déclarée reste une exigence non satisfaite.
    /// </summary>
    /// <remarks>
    /// Les six clés que <c>docs/HUMAN_REVIEW.md</c> couvre sont exactement celles qu'aucun producteur
    /// ne peut émettre. Les déclarer réussies ne les satisfait pas : une porte ne s'ouvre que sur un
    /// accomplissement vérifié automatiquement, par une rubrique serveur ou par le moteur d'examen.
    /// Une attestation humaine, si quelqu'un la saisissait, tomberait dans le premier cas et ne
    /// changerait rien — ce qui est la propriété que le protocole promet à son lecteur.
    /// </remarks>
    [Theory]
    [InlineData(MasteryPolicyCatalog.CleanGit)]
    [InlineData(MasteryPolicyCatalog.TenMinutePresentation)]
    [InlineData(MasteryPolicyCatalog.MockInterview)]
    [InlineData(MasteryPolicyCatalog.PragmaticArchitecture)]
    [InlineData(MasteryPolicyCatalog.English)]
    [InlineData(MasteryPolicyCatalog.FinalDefense)]
    public void ADeclaredHumanJudgementAchievementLeavesItsGateBlocked(string key)
    {
        Guid profileId = Guid.NewGuid();
        MasteryGatePolicy gatePolicy = MasteryPolicyCatalog.Version1.Gates
            .Single(gate => gate.Requirements.Any(requirement =>
                string.Equals(requirement.AchievementKey, key, StringComparison.Ordinal)));
        string label = gatePolicy.Requirements
            .Single(requirement => string.Equals(requirement.AchievementKey, key, StringComparison.Ordinal))
            .Label;

        MasterySnapshot snapshot = Calculate(
            profileId,
            [],
            [
                new MasteryAchievement(
                    Guid.NewGuid(),
                    profileId,
                    key,
                    MasteryVerificationKind.ManualDeclaration,
                    Passed: true,
                    DurationMinutes: 60,
                    Now.AddDays(-1),
                    "attestation:humaine"),
            ]);

        MasteryGateResult gate = snapshot.Gates.Single(item => item.Gate == gatePolicy.Gate);

        Assert.False(gate.IsOpen);
        Assert.Contains(label, gate.Blockers);
    }

    /// <summary>
    /// Une carte de révision ou un quiz ne peuvent pas devenir une preuve d'explication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// La composante Explication n'a aucun producteur, et la route la plus tentante pour lui en donner
    /// un consiste à réutiliser un moteur qui corrige déjà côté serveur : une carte à choix dont la
    /// question porte sur le <em>pourquoi</em> d'une solution, projetée en Explication plutôt qu'en
    /// rétention. Le moteur est honnête ; la projection ne l'est pas. Reconnaître la bonne réponse
    /// parmi quatre n'est pas produire un raisonnement — c'est l'acte que la composante Quiz mesure
    /// déjà, à 5 %. La reprojeter en Explication paierait deux fois le même geste, à 10 % de plus, et
    /// une carte attachée à un exercice déjà couvert alimenterait rétention <em>et</em> explication,
    /// soit 25 % du score pour un seul clic.
    /// </para>
    /// <para>
    /// La règle d'éligibilité refuse déjà ces deux routes : seul <c>ServerRubric</c> admet une
    /// observation d'explication. Ce test rend ce refus explicite pour qu'un incrément futur ne
    /// l'obtienne pas en élargissant la règle — l'élargir est possible, mais alors ce test tombe, et
    /// sa suppression est un acte visible plutôt qu'un effet de bord.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(MasteryVerificationKind.ReviewEngine, MasteryEvidenceSource.Review)]
    [InlineData(MasteryVerificationKind.QuizEngine, MasteryEvidenceSource.Quiz)]
    [InlineData(MasteryVerificationKind.AutomaticTests, MasteryEvidenceSource.Practice)]
    [InlineData(MasteryVerificationKind.ManualDeclaration, MasteryEvidenceSource.Explanation)]
    public void NoEngineOtherThanAServerRubricCanFeedTheExplanationComponent(
        MasteryVerificationKind verification,
        MasteryEvidenceSource source)
    {
        Guid profileId = Guid.NewGuid();
        MasterySnapshot snapshot = Calculate(
            profileId,
            [
                Observation(
                    profileId,
                    MasteryDomain.CSharp,
                    MasteryComponent.Explanation,
                    "csharp-why-card",
                    100m,
                    source: source,
                    verification: verification),
            ]);

        MasteryComponentScore explanation = snapshot.Domains
            .Single(domain => domain.Domain == MasteryDomain.CSharp)
            .Components
            .Single(component => component.Component == MasteryComponent.Explanation);

        Assert.False(explanation.HasEvidence);
        Assert.Equal(0m, explanation.Score);
        Assert.Equal(0, explanation.EvidenceCount);
    }

    /// <summary>
    /// L'explication sans producteur coûte dix points, et ces dix points ne bloquent rien.
    /// </summary>
    /// <remarks>
    /// C'est le fait qui doit rester sous les yeux de toute reprise tentée de fabriquer un producteur :
    /// le plafond de 90 qui en résulte reste au-dessus du seuil ordinaire (80) comme du seuil critique
    /// (85). Aucun domaine, aucune porte n'est fermé par cette absence. Le prix payé est un score qui
    /// n'atteint jamais cent — non un parcours bloqué — et il ne justifie donc pas d'admettre une
    /// preuve qui mesurerait autre chose que ce que la composante nomme.
    /// </remarks>
    [Fact]
    public void LosingTheExplanationWeightStillClearsBothThresholds()
    {
        MasteryPolicy policy = MasteryPolicyCatalog.Version1;
        decimal explanationWeight = policy.Components
            .Single(component => component.Component == MasteryComponent.Explanation)
            .Weight;
        decimal ceiling = 100m * (policy.Components.Sum(component => component.Weight) - explanationWeight);

        Assert.Equal(0.10m, explanationWeight);
        Assert.Equal(90m, ceiling);
        Assert.True(ceiling > policy.CriticalModuleThreshold);
        Assert.True(ceiling > policy.ModuleThreshold);
    }

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
