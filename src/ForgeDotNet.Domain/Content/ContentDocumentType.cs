namespace ForgeDotNet.Domain.Content;

public enum ContentDocumentType
{
    Lesson,
    Exercise,
    Curriculum,
    DebugScenario,
    SqlScenario,
    InterviewQuestion,
    EnglishActivity,
    Project,

    /// <summary>
    /// Banque des cartes de révision à choix rattachées aux exercices.
    /// </summary>
    ReviewCardBank,
}
