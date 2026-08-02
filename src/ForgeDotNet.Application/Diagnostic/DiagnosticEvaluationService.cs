using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Diagnostic;
using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.Diagnostic;

public sealed class DiagnosticEvaluationService(
    IDiagnosticRubricSource rubricSource,
    IDiagnosticSessionRepository sessionRepository,
    IDiagnosticEvaluationRepository evaluationRepository,
    ILocalProfileRepository profileRepository,
    DiagnosticSessionCoordinator coordinator,
    TimeProvider timeProvider)
{
    public async ValueTask<DiagnosticEvaluationView> GetOrCreateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await using var lease = await coordinator.EnterAsync(cancellationToken);
        DiagnosticEvaluationData? persisted = await evaluationRepository.GetAsync(
            profile.LocalId,
            sessionId,
            cancellationToken);
        if (persisted is not null)
        {
            return ToView(persisted);
        }

        DiagnosticSessionData session = await sessionRepository.GetAsync(
            profile.LocalId,
            sessionId,
            cancellationToken)
            ?? throw new KeyNotFoundException("La session de diagnostic demandée n'existe pas.");
        if (session.Timeline.SessionStatus == DiagnosticSessionStatus.Active || session.EndedAtUtc is null)
        {
            throw new InvalidOperationException("Le diagnostic doit être terminé ou abandonné avant son évaluation.");
        }

        ValidateResponseChronology(session);
        DiagnosticScoringRubric rubric = await rubricSource.GetRubricAsync(cancellationToken);
        if (!string.Equals(rubric.Snapshot.BankId, session.BankId, StringComparison.Ordinal)
            || rubric.Snapshot.BankVersion != session.BankVersion
            || !string.Equals(rubric.Snapshot.BankRevision, session.BankRevision, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Le barème compatible avec la révision figée de cette session n'est pas disponible.");
        }

        DiagnosticEvaluationAnswer[] answers = session.Responses
            .Select(response => new DiagnosticEvaluationAnswer(
                response.QuestionId,
                response.SelectedOptionId))
            .ToArray();
        DiagnosticEvaluationReport report = DiagnosticEvaluationRules.Evaluate(
            session.Plan,
            answers,
            rubric);
        var proposed = new DiagnosticEvaluationData(
            session.Id,
            profile.LocalId,
            report,
            timeProvider.GetUtcNow());
        DiagnosticEvaluationData stored = await evaluationRepository.CreateOrGetAsync(
            proposed,
            cancellationToken);
        return ToView(stored);
    }

    private static void ValidateResponseChronology(DiagnosticSessionData session)
    {
        DateTimeOffset endedAt = session.EndedAtUtc!.Value;
        if (endedAt < session.StartedAtUtc
            || session.Responses.Any(response =>
                response.SavedAtUtc < session.StartedAtUtc
                || response.SavedAtUtc > endedAt))
        {
            throw new InvalidDataException("La chronologie des réponses du diagnostic est incohérente.");
        }
    }

    private static DiagnosticEvaluationView ToView(DiagnosticEvaluationData evaluation)
    {
        DiagnosticEvaluationReport report = evaluation.Report;
        DiagnosticDomainEvaluationView[] domains = report.Domains
            .Select(domain => new DiagnosticDomainEvaluationView(
                DiagnosticDomains.GetId(domain.Domain),
                DiagnosticDomains.GetDisplayName(domain.Domain),
                domain.IsCritical,
                domain.PlannedQuestionCount,
                domain.AnsweredQuestionCount,
                domain.CorrectAnswerCount,
                domain.Measure.Score,
                domain.Measure.LowerBound,
                domain.Measure.UpperBound))
            .ToArray();
        DiagnosticCriticalGapView[] gaps = report.CriticalGaps
            .Select(gap => new DiagnosticCriticalGapView(
                DiagnosticDomains.GetId(gap.Domain),
                DiagnosticDomains.GetDisplayName(gap.Domain),
                gap.Reason,
                gap.Score))
            .ToArray();
        return new DiagnosticEvaluationView(
            evaluation.SessionId,
            report.Rubric.Id,
            report.Rubric.Version,
            report.Rubric.Revision,
            report.Mode,
            report.Overall.Score,
            report.Overall.LowerBound,
            report.Overall.UpperBound,
            report.Confidence,
            report.Level,
            report.IsProvisional,
            new DiagnosticReliabilityView(
                report.Reliability.CollectionComplete,
                report.Reliability.AllDomainsObserved,
                report.Reliability.FullInitialDepth,
                report.Reliability.CoveragePercent),
            Array.AsReadOnly(domains),
            Array.AsReadOnly(gaps),
            evaluation.CreatedAtUtc);
    }
}
