using ForgeDotNet.Domain.Practice;

namespace ForgeDotNet.Application.Practice;

public interface IPracticeExerciseSource
{
    ValueTask<IReadOnlyList<PracticeExercise>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<PracticeExercise?> GetAsync(string exerciseId, CancellationToken cancellationToken = default);
}
