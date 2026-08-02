namespace ForgeDotNet.Domain.Curriculum;

public static class ReadingProgress
{
    public static int CalculatePercentage(
        IEnumerable<string> observableActivityIds,
        IEnumerable<string> completedActivityIds)
    {
        ArgumentNullException.ThrowIfNull(observableActivityIds);
        ArgumentNullException.ThrowIfNull(completedActivityIds);

        string[] declared = observableActivityIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (declared.Length == 0)
        {
            return 0;
        }

        var allowed = declared.ToHashSet(StringComparer.Ordinal);
        int completed = completedActivityIds
            .Distinct(StringComparer.Ordinal)
            .Count(allowed.Contains);
        return (int)Math.Round(
            completed * 100d / declared.Length,
            MidpointRounding.AwayFromZero);
    }
}
