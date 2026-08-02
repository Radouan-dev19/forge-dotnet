namespace ForgeDotNet.Application.Curriculum;

public interface ILessonContentSource
{
    ValueTask<LessonLibraryView> GetLibraryAsync(CancellationToken cancellationToken = default);

    ValueTask<LessonContentDocument?> GetLessonAsync(
        string lessonId,
        CancellationToken cancellationToken = default);
}
