using ForgeDotNet.Application.HumanReview;
using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.Mastery;

namespace ForgeDotNet.UnitTests;

/// <summary>
/// Les règles d'acceptation d'une attestation tiennent la frontière du canal : complète, signée
/// d'un tiers qui n'est pas l'apprenant, sur une grille du protocole, tous critères obligatoires
/// observés. Chaque test couvre un contournement que la page ne doit pas laisser passer.
/// </summary>
public sealed class HumanAttestationRulesTests
{
    private static readonly DateOnly ReviewedOn = new(2026, 8, 18);

    [Fact]
    public void ACompleteThirdPartyAttestationIsAccepted()
    {
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(Draft());

        Assert.Empty(failures);
    }

    [Fact]
    public void SelfAttestationIsRefused()
    {
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(
            Draft() with { ReviewerName = "  Camille Apprenant " });

        Assert.Contains(failures, failure => failure.Contains("auto-attestation", StringComparison.Ordinal));
    }

    [Fact]
    public void AnEmptyLearnerNameIsRefusedBecauseTheCheckDependsOnIt()
    {
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(
            Draft() with { LearnerDisplayName = "  " });

        Assert.Contains(failures, failure => failure.Contains("profil", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AMachineProvableKeyIsNotAttestable()
    {
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(
            Draft() with { TargetKey = MasteryPolicyCatalog.EfCore });

        string failure = Assert.Single(failures);
        Assert.Contains("protocole", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void AnIncompleteGridIsRefused()
    {
        HumanAttestationDraft draft = Draft();
        IReadOnlyList<string> missing = HumanAttestationRules.Validate(
            draft with { Criteria = draft.Criteria.Skip(1).ToArray() });
        IReadOnlyList<string> silent = HumanAttestationRules.Validate(
            draft with
            {
                Criteria = draft.Criteria
                    .Select(entry => entry.Number == 3 ? entry with { Evidence = " " } : entry)
                    .ToArray(),
            });

        Assert.Contains(missing, failure => failure.Contains("exactement une fois", StringComparison.Ordinal));
        Assert.Contains(silent, failure => failure.Contains("ce qui l'a montré", StringComparison.Ordinal));
    }

    [Fact]
    public void AMandatoryCriterionNotObservedIsRefused()
    {
        HumanAttestationDraft draft = Draft();
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(
            draft with
            {
                Criteria = draft.Criteria
                    .Select(entry => entry.Number == 1 ? entry with { Observed = false } : entry)
                    .ToArray(),
            });

        Assert.Contains(failures, failure => failure.Contains("non satisfait", StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnknownCriterionNumberIsRefused()
    {
        HumanAttestationDraft draft = Draft();
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(
            draft with { Criteria = [.. draft.Criteria, new HumanAttestationCriterionEntry(99, true, "inventé")] });

        Assert.Contains(failures, failure => failure.Contains("d'autres critères", StringComparison.Ordinal));
    }

    [Fact]
    public void ADurationBelowTheGridMinimumIsRefused()
    {
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(
            Draft(MasteryPolicyCatalog.MockInterview) with { DurationMinutes = 30 });

        Assert.Contains(failures, failure => failure.Contains("45 minutes", StringComparison.Ordinal));
    }

    [Fact]
    public void AMissingNamedGapIsRefusedEvenOnAFavorableVerdict()
    {
        IReadOnlyList<string> failures = HumanAttestationRules.Validate(Draft() with { NamedGap = "" });

        Assert.Contains(failures, failure => failure.Contains("écart nommé", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheExplanationGridRequiresItsExerciseAndTheOthersRefuseOne()
    {
        IReadOnlyList<string> withoutExercise = HumanAttestationRules.Validate(
            Draft(HumanReviewCatalog.ExplanationTarget) with { ExplainedExerciseId = null });
        IReadOnlyList<string> misplacedExercise = HumanAttestationRules.Validate(
            Draft() with { ExplainedExerciseId = "algo-gcd-001" });

        Assert.Contains(withoutExercise, failure => failure.Contains("identifiant", StringComparison.Ordinal));
        Assert.Contains(misplacedExercise, failure => failure.Contains("grille d'explication", StringComparison.Ordinal));
    }

    private static HumanAttestationDraft Draft(string targetKey = MasteryPolicyCatalog.CleanGit)
    {
        HumanReviewGrid grid = HumanReviewCatalog.Find(targetKey)!;
        return new HumanAttestationDraft(
            targetKey,
            "Dominique Relecteur",
            "ancien collègue",
            "Camille Apprenant",
            ReviewedOn,
            Math.Max(grid.MinimumDurationMinutes, 45),
            "dépôt personnel, plage main~30..main, tête a1b2c3d",
            "Les messages de fusion restent génériques : nommer l'intention de chaque fusion.",
            grid.IsExplanationComponent ? "algo-binary-search-001" : null,
            grid.Criteria
                .Select(criterion => new HumanAttestationCriterionEntry(
                    criterion.Number, true, $"constaté en séance sur le critère {criterion.Number}"))
                .ToArray());
    }
}

/// <summary>
/// Le service applique les règles, refuse le rejeu d'une même revue et n'écrit rien d'invalide.
/// </summary>
public sealed class HumanReviewServiceTests
{
    [Fact]
    public async Task RecordRefusesTheReplayOfTheSameReview()
    {
        var store = new FakeStore();
        var service = new HumanReviewService(new FakeProfiles("Camille Apprenant"), store, TimeProvider.System);
        RecordHumanAttestationCommand command = Command();

        RecordHumanAttestationResult first = await service.RecordAsync(command);
        RecordHumanAttestationResult replay = await service.RecordAsync(command);

        Assert.True(first.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.Contains(replay.Failures, failure => failure.Contains("rejeu", StringComparison.Ordinal));
        Assert.Single(store.Entries);
    }

    [Fact]
    public async Task RecordWritesNothingWhenTheRulesRefuse()
    {
        var store = new FakeStore();
        var service = new HumanReviewService(new FakeProfiles("Dominique Relecteur"), store, TimeProvider.System);

        RecordHumanAttestationResult result = await service.RecordAsync(Command());

        Assert.False(result.Succeeded);
        Assert.Empty(store.Entries);
    }

    private static RecordHumanAttestationCommand Command()
    {
        HumanReviewGrid grid = HumanReviewCatalog.Find(MasteryPolicyCatalog.CleanGit)!;
        return new RecordHumanAttestationCommand(
            grid.TargetKey,
            "Dominique Relecteur",
            "collègue",
            new DateOnly(2026, 8, 18),
            45,
            "dépôt personnel, plage main~20..main",
            "Deux commits mélangent fond et style : les séparer.",
            null,
            grid.Criteria
                .Select(criterion => new HumanAttestationCriterionEntry(criterion.Number, true, "constaté"))
                .ToArray());
    }

    private sealed class FakeProfiles(string displayName) : ILocalProfileRepository
    {
        private readonly UserProfile _profile = UserProfile.CreateDefault(DateTimeOffset.UnixEpoch)
            .Update(displayName, "objectif de test", 10, InterfaceLanguage.French);

        public ValueTask<UserProfile> GetAsync(CancellationToken cancellationToken = default) => new(_profile);

        public ValueTask SaveAsync(UserProfile profile, CancellationToken cancellationToken = default) => default;
    }

    private sealed class FakeStore : IHumanAttestationStore
    {
        public List<HumanAttestationEntry> Entries { get; } = [];

        public ValueTask<IReadOnlyList<HumanAttestationEntry>> ListAsync(
            Guid profileId, CancellationToken cancellationToken = default) =>
            new(Entries.Where(entry => entry.ProfileId == profileId).ToArray());

        public ValueTask<bool> ExistsAsync(
            Guid profileId, string targetKey, DateOnly reviewedOn, CancellationToken cancellationToken = default) =>
            new(Entries.Any(entry => entry.ProfileId == profileId
                && entry.TargetKey == targetKey
                && entry.ReviewedOn == reviewedOn));

        public ValueTask AppendAsync(HumanAttestationEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return default;
        }
    }
}
