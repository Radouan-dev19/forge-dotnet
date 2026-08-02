using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Application.CodeRunner;

public interface IPracticeLearningAttemptRepository
{
    ValueTask AppendAsync(
        PracticeLearningAttempt attempt,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<PracticeLearningAttempt>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
