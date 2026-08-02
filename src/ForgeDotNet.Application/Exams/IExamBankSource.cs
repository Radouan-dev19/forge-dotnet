using ForgeDotNet.Domain.Exams;

namespace ForgeDotNet.Application.Exams;

public interface IExamBankSource
{
    ValueTask<IReadOnlyList<ExamBlueprint>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<ExamBlueprint?> GetAsync(string examId, CancellationToken cancellationToken = default);
}

