namespace ForgeDotNet.Application.Curriculum;

/// <summary>
/// Lit la piste senior (S25 à S32) depuis son propre manifeste de parcours,
/// <c>forge-senior-reference</c>, distinct du parcours junior. La piste est un second parcours : elle
/// ne partage ni ses compteurs, ni sa page, ni son relevé de couverture avec les vingt-quatre semaines
/// du socle, mais réutilise le même lecteur de leçons et le même bac à sable pour ses exercices.
/// </summary>
/// <remarks>
/// Les activités citées par une leçon senior sont reliées par le même résolveur que le socle : la
/// piste avait gardé des citations inertes après que le socle les avait rendues cliquables — le
/// défaut exact que <c>LessonActivityReachabilityWebTests</c> refuse désormais, document par document.
/// </remarks>
public sealed class BrowseSeniorTrack(
    ILessonContentSource seniorSource,
    ILessonActivityLinkResolver? activityLinks = null)
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
            Sections = await LessonActivityLinker.LinkAsync(document.PublicView.Sections, activityLinks, cancellationToken),
            PreviousLesson = previous,
            NextLesson = next,
        };
    }

    /// <summary>
    /// Vérifie une réponse au quiz d'une leçon senior sans rien enregistrer : la piste se lit sans
    /// suivi de progression et ce contrôle ne produit aucune preuve de maîtrise. La réponse correcte
    /// reste côté serveur ; seul le verdict et le retour éditorial reviennent.
    /// </summary>
    public async ValueTask<LessonQuizResultView?> CheckQuizAsync(
        string lessonId,
        int optionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonId);
        LessonContentDocument? document = await seniorSource.GetLessonAsync(lessonId, cancellationToken);
        if (document is null || document.QuizDefinition.PublicView.Options.All(option => option.Id != optionId))
        {
            return null;
        }

        bool correct = optionId == document.QuizDefinition.CorrectOptionId;
        return new LessonQuizResultView(
            correct,
            correct ? document.QuizDefinition.SuccessFeedback : document.QuizDefinition.RetryFeedback);
    }
}

/// <summary>Verdict d'un quiz vérifié sans enregistrement.</summary>
public sealed record LessonQuizResultView(bool IsCorrect, string Feedback);
