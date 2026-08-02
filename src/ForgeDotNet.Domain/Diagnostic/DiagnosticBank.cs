namespace ForgeDotNet.Domain.Diagnostic;

public sealed record DiagnosticOption(string Id, string Text);

public sealed record DiagnosticQuestion(
    string Id,
    DiagnosticDomain Domain,
    int Difficulty,
    string Prompt,
    IReadOnlyList<DiagnosticOption> Options);

public sealed record DiagnosticBank(
    string Id,
    int Version,
    string Revision,
    string Title,
    IReadOnlyList<DiagnosticQuestion> Questions);

public enum DiagnosticMode
{
    Initial,
    Reduced,
}

public sealed record DiagnosticPlanSection(
    int Index,
    string Title,
    IReadOnlyList<DiagnosticQuestion> Questions);

public sealed record DiagnosticPlan(
    DiagnosticMode Mode,
    int Seed,
    IReadOnlyList<DiagnosticPlanSection> Sections)
{
    public int QuestionCount => Sections.Sum(section => section.Questions.Count);
}
