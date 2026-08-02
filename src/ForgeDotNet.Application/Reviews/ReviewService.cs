using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.IdentityLocal;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.Application.Reviews;

public sealed class ReviewService(
    IReviewSourceProvider sourceProvider,
    IReviewRepository repository,
    IReviewPolicySource policySource,
    ILocalProfileRepository profileRepository,
    TimeProvider timeProvider)
{
    public async ValueTask<ReviewQueueView> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        ReviewPolicy policy = await policySource.GetActiveAsync(cancellationToken);
        TimeZoneInfo timeZone = ResolveTimeZone(policy);
        DateTimeOffset now = timeProvider.GetUtcNow();
        IReadOnlyList<ReviewSourceCandidate> candidates = await sourceProvider.ListAsync(
            profile.LocalId,
            cancellationToken);
        foreach (ReviewSourceCandidate candidate in candidates)
        {
            ReviewItem proposed = ReviewRules.Create(
                profile.LocalId,
                candidate.Source,
                candidate.Domain,
                candidate.ScheduleKind,
                candidate.Card,
                policy,
                timeZone,
                now);
            _ = await repository.CreateOrGetAsync(proposed, cancellationToken);
        }

        DateOnly today = ReviewRules.LocalDate(now, timeZone);
        ReviewItem[] items = (await repository.ListActiveAsync(profile.LocalId, cancellationToken))
            .OrderBy(item => item.DueOn)
            .ThenBy(item => item.Id)
            .ToArray();
        ReviewItemView[] due = items.Where(item => item.DueOn <= today)
            .Select(item => ToView(item, policy, today))
            .ToArray();
        ReviewItemView[] upcoming = items.Where(item => item.DueOn > today)
            .Take(10)
            .Select(item => ToView(item, policy, today))
            .ToArray();
        return new ReviewQueueView(
            policy.Id,
            policy.Version,
            policy.Revision,
            policy.TimeZoneId,
            today,
            Array.AsReadOnly(due),
            Array.AsReadOnly(upcoming));
    }

    public async ValueTask<ReviewItemView> AddPersonalCardAsync(
        PersonalReviewCardInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        ReviewPolicy policy = await policySource.GetActiveAsync(cancellationToken);
        TimeZoneInfo timeZone = ResolveTimeZone(policy);
        DateTimeOffset now = timeProvider.GetUtcNow();
        string key = $"personal:{Guid.NewGuid():N}";
        var source = new ReviewSource(
            key,
            ReviewSourceKind.Personal,
            key,
            1,
            "personal-v1",
            now);
        var card = new ReviewCard(
            input.Question,
            input.ExpectedAnswer,
            Array.Empty<ReviewChoice>(),
            ReviewEvaluationMode.ExactText,
            CanProduceMasteryEvidence: false);
        ReviewItem proposed = ReviewRules.Create(
            profile.LocalId,
            source,
            input.Domain,
            ReviewScheduleKind.General,
            card,
            policy,
            timeZone,
            now);
        ReviewItem stored = await repository.CreateOrGetAsync(proposed, cancellationToken);
        return ToView(stored, policy, ReviewRules.LocalDate(now, timeZone));
    }

    public async ValueTask<ReviewAnswerResultView> AnswerAsync(
        Guid itemId,
        int expectedVersion,
        ReviewAnswerInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(itemId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(input);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        ReviewPolicy policy = await policySource.GetActiveAsync(cancellationToken);
        TimeZoneInfo timeZone = ResolveTimeZone(policy);
        ReviewItem item = await repository.GetAsync(profile.LocalId, itemId, cancellationToken)
            ?? throw new KeyNotFoundException("La carte de révision demandée n’existe pas.");
        if (item.Version != expectedVersion)
        {
            throw new InvalidOperationException("La carte a déjà été modifiée. Recharge la file avant de répondre.");
        }

        ReviewTransition transition = ReviewRules.Answer(
            item,
            new ReviewAnswer(input.Response, input.SelfReportedSuccess),
            policy,
            timeZone,
            timeProvider.GetUtcNow());
        await repository.SaveTransitionAsync(profile.LocalId, expectedVersion, transition, cancellationToken);
        string? expectedAnswer = item.Card.ExpectedAnswer;
        if (item.Card.EvaluationMode == ReviewEvaluationMode.Choice && expectedAnswer is not null)
        {
            expectedAnswer = item.Card.Choices.Single(choice =>
                string.Equals(choice.Id, expectedAnswer, StringComparison.Ordinal)).Text;
        }

        string explanation = transition.Attempt.IsMasteryEligible
            ? "Réponse vérifiée côté serveur : cette tentative devient une preuve de rétention."
            : transition.Attempt.IsVerified
                ? "Réponse vérifiée pour le calendrier ; une carte personnelle ne modifie pas la maîtrise."
                : "Autoévaluation conservée pour le calendrier uniquement ; elle ne modifie pas la maîtrise.";
        return new ReviewAnswerResultView(
            transition.Attempt.Outcome,
            transition.Attempt.IsVerified,
            transition.Attempt.IsMasteryEligible,
            expectedAnswer,
            transition.Attempt.NextDueOn,
            transition.Attempt.NextIntervalDays,
            explanation);
    }

    private static ReviewItemView ToView(ReviewItem item, ReviewPolicy policy, DateOnly today)
    {
        string evaluation = item.Card.EvaluationMode switch
        {
            ReviewEvaluationMode.Choice => "Choix vérifié côté serveur ; la réponse attendue reste cachée jusqu’à la soumission.",
            ReviewEvaluationMode.ExactText => "Comparaison exacte, sans tenir compte de la casse ni des espaces ; planification uniquement pour une carte personnelle.",
            _ => "Réponse à blanc puis autoévaluation ; planification uniquement, sans effet sur la maîtrise.",
        };
        return new ReviewItemView(
            item.Id,
            item.Version,
            SourceLabel(item.Source.Kind),
            item.Source.ItemId,
            DomainLabel(item.Domain),
            item.Card.Question,
            Array.AsReadOnly(item.Card.Choices.Select(choice => new ReviewChoiceView(choice.Id, choice.Text)).ToArray()),
            item.Card.EvaluationMode,
            item.DueOn,
            item.DueOn <= today,
            Math.Max(0, today.DayNumber - item.DueOn.DayNumber),
            ReviewRules.CurrentIntervalDays(item, policy),
            item.AttemptCount,
            evaluation);
    }

    private static TimeZoneInfo ResolveTimeZone(ReviewPolicy policy)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(policy.TimeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Le fuseau de planification {policy.TimeZoneId} n’est pas disponible.",
                exception);
        }
    }

    private static string SourceLabel(ReviewSourceKind kind) => kind switch
    {
        ReviewSourceKind.PracticeError => "Erreur de pratique",
        ReviewSourceKind.DebuggingBug => "Bug DebugLab",
        ReviewSourceKind.SqlError => "Erreur SQL",
        ReviewSourceKind.ExamFailure => "Échec d’examen",
        ReviewSourceKind.MissedDiagnosticQuestion => "Question ratée",
        ReviewSourceKind.SolutionViewed => "Solution consultée",
        ReviewSourceKind.Personal => "Carte personnelle",
        _ => "Source inconnue",
    };

    private static string DomainLabel(MasteryDomain domain) => domain switch
    {
        MasteryDomain.CSharp => "C#",
        MasteryDomain.Debugging => "Débogage",
        MasteryDomain.Sql => "SQL",
        MasteryDomain.Api => "API",
        MasteryDomain.Tests => "Tests",
        MasteryDomain.ContinuousIntegration => "Git / intégration continue",
        MasteryDomain.English => "Anglais professionnel",
        _ => domain.ToString(),
    };
}
