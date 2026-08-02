using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Application.Diagnostic;

public sealed record DiagnosticDomainEvaluationView(
    string DomainId,
    string DisplayName,
    bool IsCritical,
    int PlannedQuestionCount,
    int AnsweredQuestionCount,
    int CorrectAnswerCount,
    decimal Score,
    decimal LowerBound,
    decimal UpperBound);

public sealed record DiagnosticCriticalGapView(
    string DomainId,
    string DisplayName,
    DiagnosticCriticalGapReason Reason,
    decimal Score);

public sealed record DiagnosticReliabilityView(
    bool CollectionComplete,
    bool AllDomainsObserved,
    bool FullInitialDepth,
    decimal CoveragePercent);

public sealed record DiagnosticEvaluationView(
    Guid SessionId,
    string RubricId,
    int RubricVersion,
    string RubricRevision,
    DiagnosticMode Mode,
    decimal Score,
    decimal LowerBound,
    decimal UpperBound,
    DiagnosticConfidence Confidence,
    DiagnosticLevel Level,
    bool IsProvisional,
    DiagnosticReliabilityView Reliability,
    IReadOnlyList<DiagnosticDomainEvaluationView> Domains,
    IReadOnlyList<DiagnosticCriticalGapView> CriticalGaps,
    DateTimeOffset CreatedAtUtc);
