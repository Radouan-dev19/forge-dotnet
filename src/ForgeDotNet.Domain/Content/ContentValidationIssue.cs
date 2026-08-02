namespace ForgeDotNet.Domain.Content;

public sealed record ContentValidationIssue(
    string Code,
    string FilePath,
    string PropertyPath,
    string Message);
