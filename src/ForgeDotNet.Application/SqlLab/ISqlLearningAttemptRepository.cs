using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.SqlLab;

public interface ISqlLearningAttemptRepository
{
    ValueTask AppendAsync(SqlLearningAttempt attempt, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<SqlLearningAttempt>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
