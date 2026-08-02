using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Application.Exams;

public sealed record SqlExamItemDefinition(
    string ItemId,
    int ItemVersion,
    string ContentRevision,
    SqlLabExpectedResult ExpectedResult,
    string SolutionQuery);

public interface IExamSqlItemSource
{
    ValueTask<SqlExamItemDefinition?> GetAsync(
        string itemId,
        int itemVersion,
        string contentRevision,
        CancellationToken cancellationToken = default);
}

public sealed record ExamRunResult(
    ExamSubmissionOutcome Outcome,
    int TotalTests,
    int PassedTests,
    int HiddenFailureCount,
    Guid DiagnosticId);

public interface ISqlExamRunner
{
    ValueTask<ExamRunResult> RunAsync(
        ExamItemSnapshot item,
        string query,
        CancellationToken cancellationToken = default);
}

public sealed class UnavailableSqlExamRunner : ISqlExamRunner
{
    public ValueTask<ExamRunResult> RunAsync(
        ExamItemSnapshot item,
        string query,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(new ExamRunResult(
            ExamSubmissionOutcome.Unavailable,
            TotalTests: 0,
            PassedTests: 0,
            HiddenFailureCount: 0,
            Guid.NewGuid()));
}
