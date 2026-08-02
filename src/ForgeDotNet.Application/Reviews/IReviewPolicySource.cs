using ForgeDotNet.Domain.Reviews;

namespace ForgeDotNet.Application.Reviews;

public interface IReviewPolicySource
{
    ValueTask<ReviewPolicy> GetActiveAsync(CancellationToken cancellationToken = default);
}

public sealed class VersionedReviewPolicySource : IReviewPolicySource
{
    public ValueTask<ReviewPolicy> GetActiveAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ReviewPolicyCatalog.Version1);
}
