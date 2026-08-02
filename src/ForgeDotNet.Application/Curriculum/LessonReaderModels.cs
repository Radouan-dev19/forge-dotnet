namespace ForgeDotNet.Application.Curriculum;

public enum LessonInlineKind
{
    Text,
    Strong,
    Code,
    Link,
}

public sealed record LessonInlineView(
    LessonInlineKind Kind,
    string Text,
    string? Href = null);

public abstract record LessonBlockView;

public sealed record LessonParagraphView(
    IReadOnlyList<LessonInlineView> Inlines) : LessonBlockView;

public sealed record LessonListView(
    bool Ordered,
    IReadOnlyList<IReadOnlyList<LessonInlineView>> Items) : LessonBlockView;

public sealed record LessonCodeView(
    string Code,
    string Language) : LessonBlockView;

public sealed record LessonSectionView(
    string Id,
    string Title,
    IReadOnlyList<LessonBlockView> Blocks);

public sealed record LessonQuizOptionView(int Id, string Text);

public sealed record LessonQuizView(
    string Id,
    string Prompt,
    IReadOnlyList<LessonQuizOptionView> Options);

public sealed record LessonNavigationLink(string Id, string Title);

public sealed record LessonView(
    string Id,
    int Version,
    string Title,
    int Week,
    int EstimatedMinutes,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<string> Skills,
    IReadOnlyList<LessonSectionView> Sections,
    LessonQuizView Quiz,
    IReadOnlyList<string> ObservableActivityIds,
    LessonNavigationLink? PreviousLesson = null,
    LessonNavigationLink? NextLesson = null);

public sealed record LessonSummaryView(
    string Id,
    string Title,
    string Summary,
    int EstimatedMinutes,
    IReadOnlyList<string> Skills);

public sealed record CurriculumModuleView(
    string Id,
    string Title,
    IReadOnlyList<LessonSummaryView> Lessons);

public sealed record LessonLibraryView(
    string Title,
    string Description,
    IReadOnlyList<CurriculumModuleView> Modules,
    string SearchQuery = "");

public sealed record LessonQuizDefinition(
    LessonQuizView PublicView,
    int CorrectOptionId,
    string SuccessFeedback,
    string RetryFeedback);

public sealed record LessonContentDocument(
    LessonView PublicView,
    LessonQuizDefinition QuizDefinition);

public sealed record LessonUserStateSnapshot(
    string Note,
    bool IsBookmarked,
    IReadOnlyList<string> CompletedActivityIds);

public sealed record LessonReaderState(
    string Note,
    bool IsBookmarked,
    IReadOnlyList<string> CompletedActivityIds,
    int ProgressPercentage);

public sealed record LessonQuizResult(
    bool IsCorrect,
    string Feedback,
    LessonReaderState State);
