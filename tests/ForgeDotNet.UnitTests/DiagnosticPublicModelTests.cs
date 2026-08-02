using ForgeDotNet.Application.Diagnostic;
using ForgeDotNet.Domain.Diagnostic;

namespace ForgeDotNet.UnitTests;

public sealed class DiagnosticPublicModelTests
{
    [Fact]
    public void SessionAndQuestionModelsKeepExpectedAnswersAndEvaluationSeparate()
    {
        string[] propertyNames = typeof(DiagnosticQuestionView)
            .GetProperties()
            .Concat(typeof(DiagnosticSessionView).GetProperties())
            .Concat(typeof(DiagnosticQuestion).GetProperties())
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Correct", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Expected", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Score", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Level", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Recommendation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EvaluationProjectionContainsNoQuestionOrExpectedAnswerDetail()
    {
        string[] propertyNames = typeof(DiagnosticEvaluationView)
            .GetProperties()
            .Concat(typeof(DiagnosticDomainEvaluationView).GetProperties())
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Expected", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Selected", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Prompt", StringComparison.OrdinalIgnoreCase)
            || name.Contains("OptionId", StringComparison.OrdinalIgnoreCase)
            || name.Contains("QuestionId", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Recommendation", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Plan", StringComparison.OrdinalIgnoreCase)
            || name.Contains("WeeklyPlan", StringComparison.OrdinalIgnoreCase));
    }
}
