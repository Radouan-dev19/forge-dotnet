namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class DiagnosticResponseRecord
{
    public Guid SessionId { get; set; }

    public required string QuestionId { get; set; }

    public required string SelectedOptionId { get; set; }

    public DateTimeOffset SavedAtUtc { get; set; }
}
