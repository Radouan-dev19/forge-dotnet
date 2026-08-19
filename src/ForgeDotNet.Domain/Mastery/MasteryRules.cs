namespace ForgeDotNet.Domain.Mastery;

public static class MasteryRules
{
    private const decimal MinimumSuccessfulEvidenceScore = 80m;

    public static MasterySnapshot Calculate(
        Guid profileId,
        MasteryPolicy policy,
        MasteryEvidenceSet evidence,
        DateTimeOffset calculatedAtUtc)
    {
        ValidatePolicy(policy);
        ArgumentNullException.ThrowIfNull(evidence);
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("Le profil de maîtrise est obligatoire.", nameof(profileId));
        }

        ValidateEvidence(profileId, evidence, calculatedAtUtc);
        MasteryObservation[] observations = evidence.Observations
            .Where(item => item.ProfileId == profileId)
            .ToArray();
        MasteryAchievement[] achievements = evidence.Achievements
            .Where(item => item.ProfileId == profileId)
            .ToArray();

        MasteryDomainScore[] domains = Enum.GetValues<MasteryDomain>()
            .Select(domain => CalculateDomain(domain, policy, observations, calculatedAtUtc))
            .ToArray();
        MasteryGateResult[] gates = CalculateGates(policy, domains, observations, achievements, calculatedAtUtc);

        return new MasterySnapshot(
            profileId,
            policy.Id,
            policy.Version,
            policy.Revision,
            evidence.Revision,
            calculatedAtUtc,
            observations.Length,
            Array.AsReadOnly(domains),
            Array.AsReadOnly(gates));
    }

    public static decimal AssistanceCap(MasteryAssistance assistance) => assistance switch
    {
        MasteryAssistance.None => 100m,
        MasteryAssistance.Hint1 => 90m,
        MasteryAssistance.Hint2 => 80m,
        MasteryAssistance.Hint3 => 70m,
        MasteryAssistance.Hint4 => 60m,
        MasteryAssistance.Solution => 0m,
        _ => throw new ArgumentOutOfRangeException(nameof(assistance)),
    };

    private static MasteryDomainScore CalculateDomain(
        MasteryDomain domain,
        MasteryPolicy policy,
        IReadOnlyCollection<MasteryObservation> allObservations,
        DateTimeOffset calculatedAtUtc)
    {
        MasteryObservation[] domainObservations = allObservations.Where(item => item.Domain == domain).ToArray();
        var componentScores = new List<MasteryComponentScore>(policy.Components.Count);
        foreach (MasteryComponentPolicy componentPolicy in policy.Components)
        {
            componentScores.Add(CalculateComponent(
                componentPolicy,
                domainObservations,
                policy,
                calculatedAtUtc));
        }

        decimal score = Round(componentScores.Sum(item => item.Score * item.Weight));
        bool critical = policy.CriticalDomains.Contains(domain);
        decimal required = critical ? policy.CriticalModuleThreshold : policy.ModuleThreshold;
        MasteryObservation[] eligible = domainObservations
            .Where(item => IsEligible(item) && EvidenceAgeDays(item, calculatedAtUtc) <= policy.MaximumEvidenceAgeDays)
            .ToArray();
        MasteryObservation[] autonomousEvidence = eligible
            .Where(item => item.Component == MasteryComponent.AutonomousPractice)
            .Where(item => !IsSolutionContaminated(item, domainObservations))
            .ToArray();
        int distinctItems = autonomousEvidence.Select(item => item.ItemId).Distinct(StringComparer.Ordinal).Count();
        bool hasRecent = autonomousEvidence.Any(item =>
            item.Assistance == MasteryAssistance.None
            && item.Score >= MinimumSuccessfulEvidenceScore
            && EvidenceAgeDays(item, calculatedAtUtc) <= policy.RecentProofDays);
        bool hasExam = eligible.Any(item => item.Component == MasteryComponent.UnassistedExam);
        var blockers = new List<string>();
        if (score < required)
        {
            blockers.Add($"Score {score:0.##}, inférieur au seuil {required:0.##}.");
        }

        if (distinctItems < policy.MinimumDistinctItems)
        {
            blockers.Add($"Variété insuffisante : {distinctItems}/{policy.MinimumDistinctItems} items distincts.");
        }

        if (!hasRecent)
        {
            blockers.Add($"Aucune preuve récente, vérifiée et sans aide sur {policy.RecentProofDays} jours.");
        }

        if (!hasExam)
        {
            blockers.Add("Aucun examen final vérifié et sans aide.");
        }

        return new MasteryDomainScore(
            domain,
            score,
            required,
            critical,
            blockers.Count == 0,
            hasRecent,
            hasExam,
            distinctItems,
            Array.AsReadOnly(componentScores.ToArray()),
            Array.AsReadOnly(blockers.ToArray()));
    }

    private static MasteryComponentScore CalculateComponent(
        MasteryComponentPolicy componentPolicy,
        IReadOnlyCollection<MasteryObservation> domainObservations,
        MasteryPolicy policy,
        DateTimeOffset calculatedAtUtc)
    {
        MasteryObservation[] eligible = domainObservations
            .Where(item => item.Component == componentPolicy.Component)
            .Where(IsEligible)
            .Where(item => EvidenceAgeDays(item, calculatedAtUtc) <= policy.MaximumEvidenceAgeDays)
            .ToArray();
        if (eligible.Length == 0)
        {
            return new MasteryComponentScore(componentPolicy.Component, componentPolicy.Weight, 0m, false, 0, 0);
        }

        decimal[] itemScores = eligible
            .GroupBy(item => item.ItemId, StringComparer.Ordinal)
            .Select(group => CalculateItemScore(
                componentPolicy.Component,
                group.Key,
                group.ToArray(),
                domainObservations,
                policy,
                calculatedAtUtc))
            .ToArray();
        return new MasteryComponentScore(
            componentPolicy.Component,
            componentPolicy.Weight,
            Round(itemScores.Average()),
            true,
            eligible.Length,
            itemScores.Length);
    }

    private static decimal CalculateItemScore(
        MasteryComponent component,
        string itemId,
        IReadOnlyCollection<MasteryObservation> observations,
        IReadOnlyCollection<MasteryObservation> domainObservations,
        MasteryPolicy policy,
        DateTimeOffset calculatedAtUtc)
    {
        if (component == MasteryComponent.AutonomousPractice
            && domainObservations.Any(item =>
                string.Equals(item.ItemId, itemId, StringComparison.Ordinal)
                && item.Assistance == MasteryAssistance.Solution))
        {
            return 0m;
        }

        MasteryObservation[] ordered = observations.OrderByDescending(item => item.ObservedAtUtc).ToArray();
        decimal weightedScore = 0m;
        decimal totalWeight = 0m;
        for (int index = 0; index < ordered.Length; index++)
        {
            MasteryObservation observation = ordered[index];
            decimal repetitionWeight = index switch
            {
                0 => 1m,
                1 => 0.5m,
                _ => 0.25m,
            };
            decimal recencyWeight = EvidenceAgeDays(observation, calculatedAtUtc) <= policy.RecentProofDays ? 1m : 0.5m;
            decimal weight = repetitionWeight * recencyWeight;
            decimal effectiveScore = Math.Min(observation.Score, AssistanceCap(observation.Assistance));
            weightedScore += effectiveScore * weight;
            totalWeight += weight;
        }

        return totalWeight == 0m ? 0m : Round(weightedScore / totalWeight);
    }

    private static MasteryGateResult[] CalculateGates(
        MasteryPolicy policy,
        IReadOnlyCollection<MasteryDomainScore> domains,
        IReadOnlyCollection<MasteryObservation> observations,
        IReadOnlyCollection<MasteryAchievement> achievements,
        DateTimeOffset calculatedAtUtc)
    {
        var results = new List<MasteryGateResult>(policy.Gates.Count);
        int unassistedCount = observations
            .Where(IsEligible)
            .Where(item => item.Component == MasteryComponent.AutonomousPractice)
            .Where(item => item.Assistance == MasteryAssistance.None)
            .Where(item => item.Score >= MinimumSuccessfulEvidenceScore)
            .Where(item => EvidenceAgeDays(item, calculatedAtUtc) <= policy.MaximumEvidenceAgeDays)
            .Where(item => !IsSolutionContaminated(item, observations))
            .Select(item => $"{item.Domain}:{item.ItemId}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        MasteryAchievement[] verifiedAchievements = achievements
            .Where(IsVerifiedAchievement)
            .ToArray();

        foreach (MasteryGatePolicy gatePolicy in policy.Gates.OrderBy(item => item.Gate))
        {
            var blockers = new List<string>();
            foreach (MasteryGateRequirement requirement in gatePolicy.Requirements)
            {
                bool passed = requirement.Kind switch
                {
                    MasteryGateRequirementKind.PreviousGate => results.Any(item =>
                        item.Gate == requirement.PreviousGate && item.IsOpen),
                    MasteryGateRequirementKind.DomainScore => domains.Any(item =>
                        item.Domain == requirement.Domain && item.Score >= requirement.MinimumScore),
                    MasteryGateRequirementKind.UnassistedExerciseCount => unassistedCount >= requirement.MinimumCount,
                    MasteryGateRequirementKind.Achievement => verifiedAchievements.Any(item =>
                        string.Equals(item.Key, requirement.AchievementKey, StringComparison.Ordinal)
                        && item.DurationMinutes >= requirement.MinimumDurationMinutes),
                    _ => false,
                };
                if (!passed)
                {
                    blockers.Add(requirement.Label);
                }
            }

            results.Add(new MasteryGateResult(
                gatePolicy.Gate,
                gatePolicy.Label,
                blockers.Count == 0,
                Array.AsReadOnly(blockers.ToArray())));
        }

        return results.ToArray();
    }

    private static bool IsEligible(MasteryObservation observation) => observation.Verification switch
    {
        MasteryVerificationKind.ManualDeclaration => false,
        MasteryVerificationKind.AutomaticTests => observation.Component == MasteryComponent.AutonomousPractice,
        MasteryVerificationKind.ServerRubric => observation.Component is MasteryComponent.AutonomousPractice or MasteryComponent.Explanation,
        MasteryVerificationKind.ExamEngine => observation.Component == MasteryComponent.UnassistedExam
            && observation.Source == MasteryEvidenceSource.Exam
            && observation.Assistance == MasteryAssistance.None,
        MasteryVerificationKind.ReviewEngine => observation.Component == MasteryComponent.SpacedRetention
            && observation.Source == MasteryEvidenceSource.Review,
        MasteryVerificationKind.QuizEngine => observation.Component == MasteryComponent.Quiz
            && observation.Source == MasteryEvidenceSource.Quiz,
        // L'explication est la seule composante à jugement humain : produire un compte rendu causal
        // n'a pas de substitut machine, et c'est un lecteur qui l'atteste — jamais l'apprenant.
        MasteryVerificationKind.HumanAttestation => observation.Component == MasteryComponent.Explanation
            && observation.Source == MasteryEvidenceSource.Explanation,
        _ => false,
    };

    private static bool IsVerifiedAchievement(MasteryAchievement achievement) =>
        achievement.Passed
        && (achievement.Verification is MasteryVerificationKind.AutomaticTests
                or MasteryVerificationKind.ServerRubric
                or MasteryVerificationKind.ExamEngine
            // Une attestation humaine ne vaut que pour les exigences qu'aucune machine ne peut
            // juger : sur toute autre clé, elle remplacerait une preuve exécutée par une parole.
            || (achievement.Verification == MasteryVerificationKind.HumanAttestation
                && MasteryPolicyCatalog.HumanJudgementKeys.Contains(achievement.Key, StringComparer.Ordinal)));

    private static bool IsSolutionContaminated(
        MasteryObservation observation,
        IReadOnlyCollection<MasteryObservation> allObservations) => allObservations.Any(item =>
            item.Domain == observation.Domain
            && string.Equals(item.ItemId, observation.ItemId, StringComparison.Ordinal)
            && item.Assistance == MasteryAssistance.Solution);

    private static int EvidenceAgeDays(MasteryObservation observation, DateTimeOffset calculatedAtUtc) =>
        EvidenceAgeDays(observation.ObservedAtUtc, calculatedAtUtc);

    private static int EvidenceAgeDays(DateTimeOffset observedAtUtc, DateTimeOffset calculatedAtUtc) =>
        (int)Math.Floor((calculatedAtUtc - observedAtUtc).TotalDays);

    private static void ValidateEvidence(
        Guid profileId,
        MasteryEvidenceSet evidence,
        DateTimeOffset calculatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(evidence.Revision) || evidence.Revision.Length > 80)
        {
            throw new ArgumentException("La révision des preuves est invalide.", nameof(evidence));
        }

        Guid[] ids = evidence.Observations.Select(item => item.Id)
            .Concat(evidence.Achievements.Select(item => item.Id))
            .ToArray();
        if (ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
        {
            throw new InvalidOperationException("Une preuve vide ou rejouée a été détectée.");
        }

        foreach (MasteryObservation observation in evidence.Observations)
        {
            if (observation.ProfileId != profileId
                || !Enum.IsDefined(observation.Domain)
                || !Enum.IsDefined(observation.Component)
                || !Enum.IsDefined(observation.Source)
                || !Enum.IsDefined(observation.Verification)
                || !Enum.IsDefined(observation.Assistance)
                || string.IsNullOrWhiteSpace(observation.ItemId)
                || observation.ItemId.Length > 160
                || observation.ItemVersion < 1
                || string.IsNullOrWhiteSpace(observation.ContentRevision)
                || observation.ContentRevision.Length > 80
                || observation.Score is < 0m or > 100m
                || string.IsNullOrWhiteSpace(observation.EvidenceReference)
                || observation.EvidenceReference.Length > 160
                || observation.ObservedAtUtc > calculatedAtUtc.AddMinutes(5))
            {
                throw new InvalidOperationException("Une observation de maîtrise est invalide.");
            }
        }

        foreach (MasteryAchievement achievement in evidence.Achievements)
        {
            if (achievement.ProfileId != profileId
                || !Enum.IsDefined(achievement.Verification)
                || string.IsNullOrWhiteSpace(achievement.Key)
                || achievement.Key.Length > 120
                || achievement.DurationMinutes is < 0 or > 1_440
                || string.IsNullOrWhiteSpace(achievement.EvidenceReference)
                || achievement.EvidenceReference.Length > 160
                || achievement.ObservedAtUtc > calculatedAtUtc.AddMinutes(5))
            {
                throw new InvalidOperationException("Une preuve de porte est invalide.");
            }
        }
    }

    private static void ValidatePolicy(MasteryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (string.IsNullOrWhiteSpace(policy.Id)
            || policy.Version < 1
            || string.IsNullOrWhiteSpace(policy.Revision)
            || policy.Components.Count != Enum.GetValues<MasteryComponent>().Length
            || policy.Components.Any(item => !Enum.IsDefined(item.Component))
            || policy.Components.Select(item => item.Component).Distinct().Count() != policy.Components.Count
            || policy.Components.Any(item => item.Weight is <= 0m or > 1m)
            || policy.Components.Sum(item => item.Weight) != 1m
            || policy.ModuleThreshold is < 0m or > 100m
            || policy.CriticalModuleThreshold is < 0m or > 100m
            || policy.CriticalDomains.Any(item => !Enum.IsDefined(item))
            || policy.CriticalDomains.Distinct().Count() != policy.CriticalDomains.Count
            || policy.MinimumDistinctItems < 1
            || policy.RecentProofDays < 1
            || policy.MaximumEvidenceAgeDays < policy.RecentProofDays
            || policy.Gates.Count != Enum.GetValues<MasteryGate>().Length
            || policy.Gates.Any(item => !Enum.IsDefined(item.Gate))
            || policy.Gates.Select(item => item.Gate).Distinct().Count() != policy.Gates.Count)
        {
            throw new InvalidOperationException("La politique de maîtrise est invalide.");
        }
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
