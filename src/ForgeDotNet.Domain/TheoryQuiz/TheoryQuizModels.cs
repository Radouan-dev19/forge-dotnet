namespace ForgeDotNet.Domain.TheoryQuiz;

/// <summary>
/// Banque de quiz théorique de niveau senior : des questions à choix multiples où une ou plusieurs
/// réponses sont justes, publiées hors parcours pour sonder la solidité théorique d'un profil
/// opérationnel.
/// </summary>
/// <remarks>
/// Une banque est un manifeste plat du catalogue, comme une banque de cartes de révision, mais elle
/// n'alimente aucune file et n'enregistre rien : la réponse est vérifiée dans la session de
/// navigation puis oubliée. <see cref="Order"/> ordonne les banques dans l'onglet.
/// </remarks>
public sealed record TheoryQuizBank(
    string Id,
    int Version,
    string Title,
    string Summary,
    int Order,
    IReadOnlyList<TheoryQuizQuestion> Questions);

public sealed record TheoryQuizOption(string Id, string Text);

/// <summary>
/// Question à réponses multiples : la sélection n'est juste que si elle coïncide exactement avec
/// l'ensemble des bonnes réponses — ni oubli, ni ajout.
/// </summary>
public sealed record TheoryQuizQuestion(
    string Id,
    string Topic,
    string Prompt,
    IReadOnlyList<TheoryQuizOption> Options,
    IReadOnlyList<string> CorrectOptionIds,
    string Rationale)
{
    /// <summary>Nombre de bonnes réponses à trouver, annoncé au répondant.</summary>
    public int CorrectCount => CorrectOptionIds.Count;

    /// <summary>
    /// Compare la sélection à la clé. Une sélection vide, un identifiant inconnu ou une réponse
    /// partielle sont tous des échecs : le niveau visé ne tolère pas l'à-peu-près.
    /// </summary>
    public TheoryQuizVerdict Evaluate(IEnumerable<string> selectedOptionIds)
    {
        ArgumentNullException.ThrowIfNull(selectedOptionIds);

        var known = new HashSet<string>(Options.Select(option => option.Id), StringComparer.Ordinal);
        var selected = new HashSet<string>(StringComparer.Ordinal);
        bool unknownSelected = false;
        foreach (string id in selectedOptionIds)
        {
            if (known.Contains(id))
            {
                selected.Add(id);
            }
            else
            {
                unknownSelected = true;
            }
        }

        var expected = new HashSet<string>(CorrectOptionIds, StringComparer.Ordinal);
        string[] missing = expected.Where(id => !selected.Contains(id)).Order(StringComparer.Ordinal).ToArray();
        string[] wrong = selected.Where(id => !expected.Contains(id)).Order(StringComparer.Ordinal).ToArray();
        bool isCorrect = !unknownSelected && selected.Count > 0 && missing.Length == 0 && wrong.Length == 0;

        return new TheoryQuizVerdict(isCorrect, missing, wrong);
    }
}

/// <summary>Résultat d'une vérification : juste, ou ce qui manque et ce qui est en trop.</summary>
public sealed record TheoryQuizVerdict(
    bool IsCorrect,
    IReadOnlyList<string> MissingOptionIds,
    IReadOnlyList<string> WrongOptionIds);

/// <summary>
/// Lecture d'un tableau de score : un quiz senior s'estime réussi au-delà d'une barre haute,
/// annoncée avant de commencer.
/// </summary>
public static class TheoryQuizScore
{
    /// <summary>Pourcentage de bonnes réponses en dessous duquel la théorie n'est pas jugée acquise.</summary>
    public const int SeniorBarPercent = 80;

    public static int Percentage(int correct, int total)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(correct);
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        if (total == 0)
        {
            return 0;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(correct, total);
        return (int)Math.Round(correct * 100.0 / total, MidpointRounding.AwayFromZero);
    }

    public static TheoryQuizReading Read(int answered, int correct, int total)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(answered);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(answered, total);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(correct, answered);
        if (answered == 0)
        {
            return TheoryQuizReading.NotStarted;
        }

        if (answered < total)
        {
            return TheoryQuizReading.InProgress;
        }

        return Percentage(correct, total) >= SeniorBarPercent
            ? TheoryQuizReading.AtSeniorBar
            : TheoryQuizReading.BelowSeniorBar;
    }
}

public enum TheoryQuizReading
{
    NotStarted,
    InProgress,
    BelowSeniorBar,
    AtSeniorBar,
}
