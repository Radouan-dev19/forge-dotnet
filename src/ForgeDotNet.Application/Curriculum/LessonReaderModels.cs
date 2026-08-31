namespace ForgeDotNet.Application.Curriculum;

public enum LessonInlineKind
{
    Text,
    Strong,
    Code,
    Link,

    /// <summary>
    /// Identifiant d'activité publiée (exercice, scénario SQL ou de débogage, projet, laboratoire,
    /// leçon) cité en code inline et résolu vers la page qui le sert.
    /// </summary>
    /// <remarks>
    /// Les leçons écrivent « Ouvrez l'exercice <c>`api-cors-origin-001`</c> dans <c>`/practice`</c> » :
    /// le lecteur devait retrouver l'activité à la main dans l'index. Le lien direct s'ouvre dans un
    /// nouvel onglet pour revenir finir la leçon sans perdre sa position.
    /// </remarks>
    ActivityLink,
}

public sealed record LessonInlineView(
    LessonInlineKind Kind,
    string Text,
    string? Href = null,
    bool OpenInNewTab = false);

/// <summary>Cible résolue d'un identifiant d'activité cité dans une leçon.</summary>
public sealed record LessonActivityLink(string Href, bool OpenInNewTab);

/// <summary>
/// Résout un identifiant cité en code inline vers la page qui sert l'activité, s'il en existe une.
/// </summary>
public interface ILessonActivityLinkResolver
{
    ValueTask<LessonActivityLink?> ResolveAsync(string identifier, CancellationToken cancellationToken = default);
}

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
