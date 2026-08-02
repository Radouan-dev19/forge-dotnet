using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Application.Practice;

public interface IPracticeActivityRepository
{
    ValueTask<PracticeActivity?> GetAsync(
        Guid profileId,
        string exerciseId,
        CancellationToken cancellationToken = default);

    ValueTask<PracticeActivity> CreateOrGetAsync(
        PracticeActivity activity,
        CancellationToken cancellationToken = default);

    ValueTask<PracticeActivity> SaveAsync(
        PracticeActivity activity,
        int expectedVersion,
        CancellationToken cancellationToken = default);
}
