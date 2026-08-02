using ForgeDotNet.Domain.Exams;

namespace ForgeDotNet.Application.Exams;

public interface IExamRepository
{
    ValueTask<ExamAttempt?> GetAsync(
        Guid profileId,
        Guid attemptId,
        CancellationToken cancellationToken = default);

    ValueTask<ExamAttempt?> GetActiveAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ExamAttempt>> ListAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    ValueTask<ExamAttempt> CreateAsync(
        ExamAttempt attempt,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ExamSubmission>> ListSubmissionsAsync(
        Guid profileId,
        Guid attemptId,
        CancellationToken cancellationToken = default);

    ValueTask<ExamAttempt> SaveSubmissionAsync(
        Guid profileId,
        int expectedVersion,
        ExamAttempt updatedAttempt,
        ExamSubmission submission,
        CancellationToken cancellationToken = default);

    ValueTask<ExamCompletion> SaveCompletionAsync(
        Guid profileId,
        int expectedVersion,
        ExamCompletion completion,
        CancellationToken cancellationToken = default);

    ValueTask<ExamReport?> GetReportAsync(
        Guid profileId,
        Guid attemptId,
        CancellationToken cancellationToken = default);
}

public interface IExamAccessPolicy
{
    ValueTask<bool> IsLearningAidLockedAsync(
        Guid profileId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}

