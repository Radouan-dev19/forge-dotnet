using ForgeDotNet.Application.IdentityLocal;
using ForgeDotNet.Domain.Curriculum;
using ForgeDotNet.Domain.IdentityLocal;

namespace ForgeDotNet.Application.Curriculum;

public sealed class GetLessonReaderState(
    BrowseLessons browseLessons,
    ILocalProfileRepository profileRepository,
    ILessonUserStateRepository stateRepository)
{
    public async ValueTask<LessonReaderState> ExecuteAsync(
        string lessonId,
        CancellationToken cancellationToken = default)
    {
        LessonView lesson = await RequireLessonAsync(browseLessons, lessonId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        LessonUserStateSnapshot state = await stateRepository.GetAsync(
            profile.LocalId,
            lessonId,
            cancellationToken);
        return ToReaderState(lesson, state);
    }

    internal static LessonReaderState ToReaderState(
        LessonView lesson,
        LessonUserStateSnapshot state) => new(
            state.Note,
            state.IsBookmarked,
            state.CompletedActivityIds,
            ReadingProgress.CalculatePercentage(
                lesson.ObservableActivityIds,
                state.CompletedActivityIds));

    internal static async ValueTask<LessonView> RequireLessonAsync(
        BrowseLessons browseLessons,
        string lessonId,
        CancellationToken cancellationToken)
    {
        LessonView? lesson = await browseLessons.GetLessonAsync(lessonId, cancellationToken);
        return lesson ?? throw new KeyNotFoundException("La leçon demandée n'existe pas dans le catalogue publié.");
    }
}

public sealed class SaveLessonNote(
    BrowseLessons browseLessons,
    ILocalProfileRepository profileRepository,
    ILessonUserStateRepository stateRepository)
{
    public const int MaximumNoteLength = 4_000;

    public async ValueTask ExecuteAsync(
        string lessonId,
        string note,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);
        if (note.Length > MaximumNoteLength)
        {
            throw new ArgumentException(
                $"La note ne peut pas dépasser {MaximumNoteLength} caractères.",
                nameof(note));
        }

        _ = await GetLessonReaderState.RequireLessonAsync(browseLessons, lessonId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await stateRepository.SaveNoteAsync(profile.LocalId, lessonId, note, cancellationToken);
    }
}

public sealed class SetLessonBookmark(
    BrowseLessons browseLessons,
    ILocalProfileRepository profileRepository,
    ILessonUserStateRepository stateRepository)
{
    public async ValueTask ExecuteAsync(
        string lessonId,
        bool isBookmarked,
        CancellationToken cancellationToken = default)
    {
        _ = await GetLessonReaderState.RequireLessonAsync(browseLessons, lessonId, cancellationToken);
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await stateRepository.SetBookmarkAsync(
            profile.LocalId,
            lessonId,
            isBookmarked,
            cancellationToken);
    }
}

public sealed class RecordLessonSectionRead(
    BrowseLessons browseLessons,
    ILocalProfileRepository profileRepository,
    ILessonUserStateRepository stateRepository)
{
    public async ValueTask<LessonReaderState> ExecuteAsync(
        string lessonId,
        string sectionId,
        CancellationToken cancellationToken = default)
    {
        LessonView lesson = await GetLessonReaderState.RequireLessonAsync(
            browseLessons,
            lessonId,
            cancellationToken);
        if (!lesson.Sections.Any(section => string.Equals(section.Id, sectionId, StringComparison.Ordinal)))
        {
            throw new ArgumentException("La section ne fait pas partie de la leçon.", nameof(sectionId));
        }

        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        await stateRepository.AddCompletedActivityAsync(
            profile.LocalId,
            lessonId,
            $"section:{sectionId}",
            cancellationToken);
        LessonUserStateSnapshot state = await stateRepository.GetAsync(
            profile.LocalId,
            lessonId,
            cancellationToken);
        return GetLessonReaderState.ToReaderState(lesson, state);
    }
}

public sealed class SubmitLessonQuiz(
    BrowseLessons browseLessons,
    ILessonContentSource contentSource,
    ILocalProfileRepository profileRepository,
    ILessonUserStateRepository stateRepository)
{
    public async ValueTask<LessonQuizResult> ExecuteAsync(
        string lessonId,
        int selectedOptionId,
        CancellationToken cancellationToken = default)
    {
        LessonView lesson = await GetLessonReaderState.RequireLessonAsync(
            browseLessons,
            lessonId,
            cancellationToken);
        LessonContentDocument document = await contentSource.GetLessonAsync(lessonId, cancellationToken)
            ?? throw new KeyNotFoundException("La leçon demandée n'existe pas dans le catalogue publié.");
        if (!document.QuizDefinition.PublicView.Options.Any(option => option.Id == selectedOptionId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectedOptionId),
                "La réponse ne fait pas partie du quiz.");
        }

        bool isCorrect = selectedOptionId == document.QuizDefinition.CorrectOptionId;
        UserProfile profile = await profileRepository.GetAsync(cancellationToken);
        if (isCorrect)
        {
            await stateRepository.AddCompletedActivityAsync(
                profile.LocalId,
                lessonId,
                $"quiz:{document.QuizDefinition.PublicView.Id}",
                cancellationToken);
        }

        LessonUserStateSnapshot state = await stateRepository.GetAsync(
            profile.LocalId,
            lessonId,
            cancellationToken);
        return new LessonQuizResult(
            isCorrect,
            isCorrect
                ? document.QuizDefinition.SuccessFeedback
                : document.QuizDefinition.RetryFeedback,
            GetLessonReaderState.ToReaderState(lesson, state));
    }
}
