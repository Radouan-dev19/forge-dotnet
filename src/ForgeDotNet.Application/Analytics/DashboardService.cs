using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Application.Mastery;
using ForgeDotNet.Application.Reviews;
using ForgeDotNet.Domain.Analytics;
using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.Analytics;

public sealed class DashboardService(
    IAnalyticsEvidenceSource evidenceSource,
    ILocalProfileRepository profileRepository,
    MasteryService masteryService,
    ReviewService reviewService,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromMinutes(5);

    public async ValueTask<LearningDashboardView> GetAsync(CancellationToken cancellationToken = default)
    {
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        AnalyticsEvidence evidence = await evidenceSource.ReadAsync(profile.LocalId, cancellationToken);
        AnalyticsSnapshot analytics = AnalyticsRules.Calculate(
            evidence,
            InactivityThreshold,
            timeProvider.GetUtcNow());
        MasteryDashboardView mastery = await masteryService.GetAsync(cancellationToken);
        ReviewQueueView reviews = await reviewService.GetQueueAsync(cancellationToken);
        MasteryDomainView[] measuredDomains = mastery.Domains
            .Where(domain => domain.Components.Any(component => component.HasEvidence))
            .ToArray();
        DashboardDomainView[] strengths = measuredDomains
            .OrderByDescending(domain => domain.Score)
            .ThenBy(domain => domain.Label, StringComparer.Ordinal)
            .Take(3)
            .Select(ToDomainView)
            .ToArray();
        DashboardDomainView[] weaknesses = measuredDomains
            .OrderBy(domain => domain.Score)
            .ThenBy(domain => domain.Label, StringComparer.Ordinal)
            .Take(3)
            .Select(ToDomainView)
            .ToArray();
        DateOnly? nextReview = reviews.DueItems.Select(item => (DateOnly?)item.DueOn)
            .Concat(reviews.UpcomingItems.Select(item => (DateOnly?)item.DueOn))
            .Min();
        return new LearningDashboardView(
            analytics.InactivityThresholdMinutes,
            analytics.ActiveIntervalCount == 0 ? null : analytics.ObservedActiveMinutes,
            analytics.ActiveIntervalCount,
            analytics.FirstAttemptSuccessRate,
            analytics.BeforeSolutionSuccessRate,
            analytics.AttemptCount,
            analytics.HintUsageCount,
            analytics.SolutionViewCount,
            reviews.DueItems.Count,
            nextReview,
            analytics.NextObjective,
            analytics.CompletedExamCount,
            analytics.AbandonedExamCount,
            analytics.TimedOutExamCount,
            analytics.AverageExamScore,
            Array.AsReadOnly(strengths),
            Array.AsReadOnly(weaknesses),
            mastery.Gates,
            "Mesures locales dérivées d’événements persistés. Un intervalle supérieur à cinq minutes est exclu du temps actif ; une donnée insuffisante reste indisponible.");
    }

    private static DashboardDomainView ToDomainView(MasteryDomainView domain) =>
        new(domain.Label, domain.Score, domain.RequiredScore);
}
