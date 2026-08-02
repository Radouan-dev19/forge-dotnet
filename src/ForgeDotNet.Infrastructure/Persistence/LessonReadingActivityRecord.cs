namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class LessonReadingActivityRecord
{
    public Guid ProfileId { get; set; }

    public string LessonId { get; set; } = string.Empty;

    public string ActivityId { get; set; } = string.Empty;

    public DateTimeOffset CompletedAtUtc { get; set; }
}
