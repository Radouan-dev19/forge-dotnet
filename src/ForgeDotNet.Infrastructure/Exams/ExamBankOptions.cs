namespace ForgeDotNet.Infrastructure.Exams;

public sealed class ExamBankOptions
{
    public required string ContentRootPath { get; init; }

    public required string BankDirectoryPath { get; init; }
}

