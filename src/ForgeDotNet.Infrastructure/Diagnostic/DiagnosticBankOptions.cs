namespace ForgeDotNet.Infrastructure.Diagnostic;

public sealed class DiagnosticBankOptions
{
    public required string ContentRootPath { get; init; }

    public required string BankDirectoryPath { get; init; }

    public string QuestionsFileName { get; init; } = "questions.json";

    public string AnswerKeyFileName { get; init; } = "answer-key.json";

    public string RubricFileName { get; init; } = "rubric.json";
}
