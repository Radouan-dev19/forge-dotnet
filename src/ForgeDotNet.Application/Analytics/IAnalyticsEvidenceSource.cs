using ForgeDotNet.Domain.Analytics;

namespace ForgeDotNet.Application.Analytics;

public interface IAnalyticsEvidenceSource
{
    ValueTask<AnalyticsEvidence> ReadAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}

