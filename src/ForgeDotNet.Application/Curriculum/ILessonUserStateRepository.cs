namespace ForgeDotNet.Application.Curriculum;

public interface ILessonUserStateRepository
{
    ValueTask<LessonUserStateSnapshot> GetAsync(
        Guid profileId,
        string lessonId,
        CancellationToken cancellationToken = default);

    ValueTask SaveNoteAsync(
        Guid profileId,
        string lessonId,
        string note,
        CancellationToken cancellationToken = default);

    ValueTask SetBookmarkAsync(
        Guid profileId,
        string lessonId,
        bool isBookmarked,
        CancellationToken cancellationToken = default);

    ValueTask AddCompletedActivityAsync(
        Guid profileId,
        string lessonId,
        string activityId,
        CancellationToken cancellationToken = default);
}
