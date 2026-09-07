using ForgeDotNet.Domain.TheoryQuiz;

namespace ForgeDotNet.UnitTests;

/// <summary>
/// La règle du quiz senior est stricte : une sélection n'est juste que si elle coïncide exactement
/// avec la clé. Ces tests ferment les contournements évidents — réponse partielle, réponse noyée
/// dans le « tout cocher », identifiant forgé, sélection vide.
/// </summary>
public sealed class TheoryQuizRulesTests
{
    private static readonly TheoryQuizQuestion Question = new(
        "q-1",
        "Sujet",
        "Quelles affirmations sont exactes ?",
        [
            new TheoryQuizOption("a", "Première"),
            new TheoryQuizOption("b", "Deuxième"),
            new TheoryQuizOption("c", "Troisième"),
            new TheoryQuizOption("d", "Quatrième"),
            new TheoryQuizOption("e", "Cinquième"),
        ],
        ["a", "c"],
        "Parce que la première et la troisième sont vraies.");

    [Fact]
    public void AnExactSelectionIsCorrectRegardlessOfOrderOrDuplicates()
    {
        TheoryQuizVerdict verdict = Question.Evaluate(["c", "a", "c"]);

        Assert.True(verdict.IsCorrect);
        Assert.Empty(verdict.MissingOptionIds);
        Assert.Empty(verdict.WrongOptionIds);
    }

    [Fact]
    public void APartialSelectionFailsAndNamesWhatIsMissing()
    {
        TheoryQuizVerdict verdict = Question.Evaluate(["a"]);

        Assert.False(verdict.IsCorrect);
        Assert.Equal(["c"], verdict.MissingOptionIds);
        Assert.Empty(verdict.WrongOptionIds);
    }

    [Fact]
    public void CheckingEverythingFailsAndNamesTheExtras()
    {
        TheoryQuizVerdict verdict = Question.Evaluate(["a", "b", "c", "d", "e"]);

        Assert.False(verdict.IsCorrect);
        Assert.Empty(verdict.MissingOptionIds);
        Assert.Equal(["b", "d", "e"], verdict.WrongOptionIds);
    }

    [Fact]
    public void AnEmptyOrForgedSelectionNeverPasses()
    {
        Assert.False(Question.Evaluate([]).IsCorrect);
        // Un identifiant inconnu accompagne une clé exacte : la sélection n'est pas celle proposée.
        Assert.False(Question.Evaluate(["a", "c", "z"]).IsCorrect);
    }

    [Fact]
    public void CorrectCountAnnouncesHowManyAnswersToFind()
    {
        Assert.Equal(2, Question.CorrectCount);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(7, 10, 70)]
    [InlineData(8, 10, 80)]
    [InlineData(10, 10, 100)]
    [InlineData(5, 7, 71)]
    [InlineData(0, 0, 0)]
    public void PercentageRoundsToTheNearestPoint(int correct, int total, int expected)
    {
        Assert.Equal(expected, TheoryQuizScore.Percentage(correct, total));
    }

    [Fact]
    public void TheReadingOnlyJudgesACompletedBank()
    {
        Assert.Equal(TheoryQuizReading.NotStarted, TheoryQuizScore.Read(0, 0, 10));
        Assert.Equal(TheoryQuizReading.InProgress, TheoryQuizScore.Read(9, 9, 10));
        Assert.Equal(TheoryQuizReading.BelowSeniorBar, TheoryQuizScore.Read(10, 7, 10));
        Assert.Equal(TheoryQuizReading.AtSeniorBar, TheoryQuizScore.Read(10, 8, 10));
    }

    [Fact]
    public void ScoreInputsMustBeCoherent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TheoryQuizScore.Percentage(11, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TheoryQuizScore.Read(11, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TheoryQuizScore.Read(5, 6, 10));
    }
}
