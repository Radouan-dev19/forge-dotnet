using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.Application.Reviews;

public interface IReviewRepository
{
    ValueTask<ReviewItem> CreateOrGetAsync(
        ReviewItem item,
        CancellationToken cancellationToken = default);

    ValueTask<ReviewItem?> GetAsync(
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ReviewItem>> ListActiveAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    ValueTask SaveTransitionAsync(
        Guid profileId,
        int expectedVersion,
        ReviewTransition transition,
        CancellationToken cancellationToken = default);
}
