namespace ForgeDotNet.Infrastructure.Persistence;

internal sealed class LessonBookmarkRecord
{
    public Guid ProfileId { get; set; }

    public string LessonId { get; set; } = string.Empty;

    public bool IsBookmarked { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
