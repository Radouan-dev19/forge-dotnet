namespace ForgeDotNet.Application.Curriculum;

/// <summary>
/// Lit la piste senior (S25 à S32) depuis son propre manifeste de parcours,
/// <c>forge-senior-reference</c>, distinct du parcours junior. La piste est un second parcours : elle
/// ne partage ni ses compteurs, ni sa page, ni son relevé de couverture avec les vingt-quatre semaines
/// du socle, mais réutilise le même lecteur de leçons et le même bac à sable pour ses exercices.
/// </summary>
public sealed class BrowseSeniorTrack(ILessonContentSource seniorSource)
{
    public ValueTask<LessonLibraryView> GetLibraryAsync(CancellationToken cancellationToken = default) =>
        seniorSource.GetLibraryAsync(cancellationToken);

    public async ValueTask<LessonView?> GetLessonAsync(
        string lessonId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonId);
        LessonContentDocument? document = await seniorSource.GetLessonAsync(lessonId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        LessonLibraryView library = await seniorSource.GetLibraryAsync(cancellationToken);
        LessonSummaryView[] ordered = library.Modules.SelectMany(module => module.Lessons).ToArray();
        int index = Array.FindIndex(
            ordered,
            lesson => string.Equals(lesson.Id, lessonId, StringComparison.Ordinal));
        LessonNavigationLink? previous = index > 0
            ? new LessonNavigationLink(ordered[index - 1].Id, ordered[index - 1].Title)
            : null;
        LessonNavigationLink? next = index >= 0 && index + 1 < ordered.Length
            ? new LessonNavigationLink(ordered[index + 1].Id, ordered[index + 1].Title)
            : null;
        return document.PublicView with
        {
            PreviousLesson = previous,
            NextLesson = next,
        };
    }
}
