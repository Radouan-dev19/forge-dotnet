using System.Text.RegularExpressions;

namespace ForgeDotNet.PersonaTests.Harness;

/// <summary>
/// Lit l'index de la bonne réponse d'un quiz de leçon dans le contenu publié. Le harnais est
/// l'auteur du scénario : connaître la réponse lui permet de scénariser un échec puis une réussite.
/// </summary>
public static partial class QuizReader
{
    public static (int CorrectIndex, int OptionCount) Read(string lessonId)
    {
        string markdown = File.ReadAllText(Path.Combine(
            PersonaPaths.RepositoryRoot, "content", "reference", "curriculum", "lessons", lessonId, "lesson.md"));
        Match quiz = QuizRegex().Match(markdown);
        if (!quiz.Success)
        {
            throw new InvalidOperationException($"Bloc quiz introuvable dans {lessonId}.");
        }

        int options = Regex.Matches(quiz.Value, "^option=", RegexOptions.Multiline).Count;
        Match correct = Regex.Match(quiz.Value, "^correct=(\\d+)", RegexOptions.Multiline);
        if (!correct.Success)
        {
            throw new InvalidOperationException($"Index de bonne réponse introuvable dans {lessonId}.");
        }

        return (int.Parse(correct.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), options);
    }

    [GeneratedRegex(":::quiz.*?:::", RegexOptions.Singleline)]
    private static partial Regex QuizRegex();
}
