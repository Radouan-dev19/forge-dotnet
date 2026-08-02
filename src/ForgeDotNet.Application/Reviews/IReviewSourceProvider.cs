using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.Application.Reviews;

public sealed record ReviewSourceCandidate(
    ReviewSource Source,
    MasteryDomain Domain,
    ReviewScheduleKind ScheduleKind,
    ReviewCard Card);

public interface IReviewSourceProvider
{
    ValueTask<IReadOnlyList<ReviewSourceCandidate>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
