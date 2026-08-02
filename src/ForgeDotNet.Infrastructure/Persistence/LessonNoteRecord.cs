namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class LessonNoteRecord
{
    public Guid ProfileId { get; set; }

    public string LessonId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
