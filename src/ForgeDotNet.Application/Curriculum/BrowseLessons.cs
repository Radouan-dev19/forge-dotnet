using ForgeDotNet.Application.Content;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Application.Curriculum;

public sealed class BrowseLessons(
    ContentCatalogProvider catalogProvider,
    ILessonContentSource contentSource)
{
    public async ValueTask<LessonLibraryView> GetLibraryAsync(
        string? searchQuery = null,
        CancellationToken cancellationToken = default)
    {
        LessonLibraryView library = await contentSource.GetLibraryAsync(cancellationToken);
        string query = searchQuery?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            return library;
        }

        ContentCatalog catalog = catalogProvider.Current;
        var matchingIds = catalog
            .Search(query, ContentDocumentType.Lesson)
            .Select(item => item.Id)
            .Concat(catalog.GetBySkill(query)
                .Where(item => item.Type == ContentDocumentType.Lesson)
                .Select(item => item.Id))
            .ToHashSet(StringComparer.Ordinal);
        CurriculumModuleView[] modules = library.Modules
            .Select(module => module with
            {
                Lessons = Array.AsReadOnly(module.Lessons
                    .Where(lesson => matchingIds.Contains(lesson.Id))
                    .ToArray()),
            })
            .Where(module => module.Lessons.Count > 0)
            .ToArray();
        return library with
        {
            Modules = Array.AsReadOnly(modules),
            SearchQuery = query,
        };
    }

    public async ValueTask<LessonView?> GetLessonAsync(
        string lessonId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonId);
        LessonLibraryView library = await contentSource.GetLibraryAsync(cancellationToken);
        LessonContentDocument? document = await contentSource.GetLessonAsync(lessonId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        LessonSummaryView[] orderedLessons = library.Modules
            .SelectMany(module => module.Lessons)
            .ToArray();
        int index = Array.FindIndex(
            orderedLessons,
            lesson => string.Equals(lesson.Id, lessonId, StringComparison.Ordinal));
        if (index < 0)
        {
            return null;
        }

        LessonNavigationLink? previous = index > 0
            ? new LessonNavigationLink(orderedLessons[index - 1].Id, orderedLessons[index - 1].Title)
            : null;
        LessonNavigationLink? next = index + 1 < orderedLessons.Length
            ? new LessonNavigationLink(orderedLessons[index + 1].Id, orderedLessons[index + 1].Title)
            : null;
        return document.PublicView with
        {
            PreviousLesson = previous,
            NextLesson = next,
        };
    }
}
