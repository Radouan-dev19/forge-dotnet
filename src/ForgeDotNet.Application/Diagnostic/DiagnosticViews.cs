using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.Application.Diagnostic;

public sealed record DiagnosticDomainCoverageView(
    string Id,
    string DisplayName,
    int QuestionCount);

public sealed record DiagnosticOverviewView(
    string BankTitle,
    int BankVersion,
    int BankQuestionCount,
    IReadOnlyList<DiagnosticDomainCoverageView> Coverage,
    Guid? ActiveSessionId,
    DiagnosticSessionSummaryView? LatestSession);

public sealed record DiagnosticSessionSummaryView(
    Guid Id,
    DiagnosticMode Mode,
    DiagnosticSessionStatus Status,
    int AnsweredCount,
    int QuestionCount,
    bool IsComplete,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);

public sealed record DiagnosticSectionSummaryView(
    int Index,
    string Title,
    DiagnosticSectionStatus Status,
    int AnsweredCount,
    int QuestionCount);

public sealed record DiagnosticQuestionView(
    string Id,
    string DomainId,
    string DomainDisplayName,
    int Difficulty,
    string Prompt,
    IReadOnlyList<DiagnosticOption> Options,
    string? SelectedOptionId);

public sealed record DiagnosticSectionView(
    int Index,
    string Title,
    DiagnosticSectionStatus Status,
    IReadOnlyList<DiagnosticQuestionView> Questions);

public sealed record DiagnosticSessionView(
    Guid Id,
    string BankId,
    int BankVersion,
    string BankRevision,
    DiagnosticMode Mode,
    DiagnosticSessionStatus Status,
    IReadOnlyList<DiagnosticSectionSummaryView> Sections,
    DiagnosticSectionView? CurrentSection,
    int CurrentSectionIndex,
    int AnsweredCount,
    int QuestionCount,
    bool IsComplete,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SectionDeadlineUtc,
    int RemainingSeconds,
    DateTimeOffset? EndedAtUtc)
{
    public bool CanStartCurrentSection =>
        Status == DiagnosticSessionStatus.Active
        && CurrentSection?.Status == DiagnosticSectionStatus.Pending;

    public bool CanAnswer =>
        Status == DiagnosticSessionStatus.Active
        && CurrentSection?.Status == DiagnosticSectionStatus.Active;

    public bool CanFinish =>
        Status == DiagnosticSessionStatus.Active
        && Sections.All(section => section.Status is DiagnosticSectionStatus.Completed or DiagnosticSectionStatus.Expired);
}
