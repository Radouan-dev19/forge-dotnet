namespace ForgeDotNet.Domain.Analytics;

public static class AnalyticsRules
{
    public static AnalyticsSnapshot Calculate(
        AnalyticsEvidence evidence,
        TimeSpan inactivityThreshold,
        DateTimeOffset calculatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (calculatedAtUtc.Offset != TimeSpan.Zero
            || inactivityThreshold < TimeSpan.FromMinutes(1)
            || inactivityThreshold > TimeSpan.FromMinutes(30)
            || evidence.HintUsageCount < 0
            || evidence.SolutionViewCount < 0)
        {
            throw new ArgumentException("La configuration analytique est invalide.");
        }

        ValidateEvidence(evidence, calculatedAtUtc);
        TimeSpan active = TimeSpan.Zero;
        int activeIntervals = 0;
        foreach (IGrouping<string, AnalyticsActivityEvent> context in evidence.ActivityEvents
            .GroupBy(item => item.ContextKey, StringComparer.Ordinal))
        {
            DateTimeOffset[] events = context.Select(item => item.OccurredAtUtc).Distinct().Order().ToArray();
            for (int index = 1; index < events.Length; index++)
            {
                TimeSpan gap = events[index] - events[index - 1];
                if (gap > TimeSpan.Zero && gap <= inactivityThreshold)
                {
                    active += gap;
                    activeIntervals++;
                }
            }
        }

        AnalyticsAttemptEvidence[] firstAttempts = evidence.Attempts
            .GroupBy(item => item.ActivityKey, StringComparer.Ordinal)
            .Select(group => group.OrderBy(item => item.Sequence).First())
            .ToArray();
        int firstSuccess = firstAttempts.Count(item => item.Passed);
        int beforeSolutionSuccess = firstAttempts.Count(item => item.Passed && !item.SolutionViewedBefore);
        AnalyticsExamEvidence[] completed = evidence.Exams
            .Where(item => item.Status == AnalyticsExamStatus.Completed)
            .ToArray();
        return new AnalyticsSnapshot(
            (int)inactivityThreshold.TotalMinutes,
            (int)Math.Floor(active.TotalMinutes),
            activeIntervals,
            evidence.Attempts.Count,
            firstAttempts.Length,
            firstSuccess,
            beforeSolutionSuccess,
            Rate(firstSuccess, firstAttempts.Length),
            Rate(beforeSolutionSuccess, firstAttempts.Length),
            evidence.HintUsageCount,
            evidence.SolutionViewCount,
            completed.Length,
            evidence.Exams.Count(item => item.Status == AnalyticsExamStatus.Abandoned),
            evidence.Exams.Count(item => item.Status == AnalyticsExamStatus.TimedOut),
            completed.Length == 0
                ? null
                : Math.Round(completed.Average(item => item.Score), 2, MidpointRounding.AwayFromZero),
            string.IsNullOrWhiteSpace(evidence.NextObjective) ? null : evidence.NextObjective);
    }

    private static decimal? Rate(int numerator, int denominator) => denominator == 0
        ? null
        : Math.Round(100m * numerator / denominator, 2, MidpointRounding.AwayFromZero);

    private static void ValidateEvidence(AnalyticsEvidence evidence, DateTimeOffset nowUtc)
    {
        if (evidence.ActivityEvents.Any(item => string.IsNullOrWhiteSpace(item.ContextKey)
                || item.ContextKey.Length > 200
                || item.OccurredAtUtc.Offset != TimeSpan.Zero
                || item.OccurredAtUtc > nowUtc.AddMinutes(5))
            || evidence.Attempts.Any(item => string.IsNullOrWhiteSpace(item.ActivityKey)
                || item.ActivityKey.Length > 200
                || item.Sequence < 1
                || item.HighestHintLevel is < 0 or > 4
                || item.ObservedAtUtc.Offset != TimeSpan.Zero
                || item.ObservedAtUtc > nowUtc.AddMinutes(5))
            || evidence.Exams.Any(item => !Enum.IsDefined(item.Status)
                || item.Score is < 0 or > 100
                || item.EndedAtUtc.Offset != TimeSpan.Zero
                || item.EndedAtUtc > nowUtc.AddMinutes(5)))
        {
            throw new InvalidDataException("Les données analytiques sont incohérentes.");
        }
    }
}
