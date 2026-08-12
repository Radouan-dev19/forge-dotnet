using System.Runtime.ExceptionServices;
using ForgeDotNet.Application.Exams;
using ForgeDotNet.Application.SqlLab;
using ForgeDotNet.Domain.Exams;
using ForgeDotNet.Domain.Mastery;
using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.Infrastructure.Exams;

public sealed class SqlLabExamRunner(
    IExamSqlItemSource itemSource,
    ISqlLabGateway gateway) : ISqlExamRunner
{
    public async ValueTask<ExamRunResult> RunAsync(
        ExamItemSnapshot item,
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.SubmissionKind != ExamSubmissionKind.Sql || item.Domain != MasteryDomain.Sql)
        {
            throw new InvalidDataException("L’item n’est pas une soumission SQL d’examen.");
        }

        SqlExamItemDefinition definition = await itemSource.GetAsync(
            item.ItemId,
            item.ItemVersion,
            item.ContentRevision,
            cancellationToken) ?? throw new InvalidDataException(
                "La définition privée de l’item SQL d’examen est absente ou obsolète.");

        SqlLabAvailability availability = await gateway.GetAvailabilityAsync(cancellationToken);
        if (!availability.Available)
        {
            return Unavailable();
        }

        SqlLabSessionDescriptor? session = null;
        Exception? executionFailure = null;
        ExamRunResult? examResult = null;
        try
        {
            // L'examen porte son propre jeu de données : il n'emprunte aucun scénario publié.
            session = await gateway.CreateSessionAsync(cancellationToken: cancellationToken);
            SqlLabExecutionResult result = await gateway.ExecuteAsync(
                session.Id,
                query,
                definition.ExpectedResult,
                cancellationToken);
            examResult = Map(result);
        }
        catch (Exception exception)
        {
            executionFailure = exception;
        }

        if (session is not null)
        {
            try
            {
                await gateway.DestroySessionAsync(session.Id, CancellationToken.None);
            }
            catch (Exception cleanupFailure) when (executionFailure is not null)
            {
                throw new AggregateException(
                    "L’exécution SQL et le nettoyage de sa base jetable ont échoué.",
                    executionFailure,
                    cleanupFailure);
            }
            catch (Exception cleanupFailure)
            {
                throw new InvalidOperationException(
                    "Le résultat SQL est refusé car le nettoyage de sa base jetable n’est pas prouvé.",
                    cleanupFailure);
            }
        }

        if (executionFailure is not null)
        {
            ExceptionDispatchInfo.Capture(executionFailure).Throw();
        }

        return examResult ?? throw new InvalidOperationException("Le runner SQL n’a produit aucun résultat.");
    }

    private static ExamRunResult Map(SqlLabExecutionResult result)
    {
        bool executionSucceeded = result.Status == SqlLabExecutionStatus.Succeeded;
        bool validationPassed = executionSucceeded && result.Validation?.Passed == true;
        ExamSubmissionOutcome outcome = result.Status switch
        {
            SqlLabExecutionStatus.Succeeded when validationPassed => ExamSubmissionOutcome.Succeeded,
            SqlLabExecutionStatus.Succeeded => ExamSubmissionOutcome.TestsFailed,
            SqlLabExecutionStatus.TimedOut => ExamSubmissionOutcome.TimedOut,
            SqlLabExecutionStatus.Cancelled => ExamSubmissionOutcome.Cancelled,
            SqlLabExecutionStatus.Unavailable or SqlLabExecutionStatus.Failed => ExamSubmissionOutcome.Unavailable,
            SqlLabExecutionStatus.Refused or SqlLabExecutionStatus.ResultLimitExceeded =>
                ExamSubmissionOutcome.TestsFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
        int totalTests = result.Status is SqlLabExecutionStatus.Unavailable or SqlLabExecutionStatus.Failed ? 0 : 2;
        int passedTests = executionSucceeded ? 1 + (validationPassed ? 1 : 0) : 0;
        int hiddenFailures = executionSucceeded && !validationPassed ? 1 : 0;
        return new ExamRunResult(outcome, totalTests, passedTests, hiddenFailures, result.DiagnosticId);
    }

    private static ExamRunResult Unavailable() => new(
        ExamSubmissionOutcome.Unavailable,
        TotalTests: 0,
        PassedTests: 0,
        HiddenFailureCount: 0,
        Guid.NewGuid());
}
