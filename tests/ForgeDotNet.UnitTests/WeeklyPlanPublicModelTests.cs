using ForgeDotNet.Application.WeeklyPlanning;

namespace ForgeDotNet.UnitTests;

public sealed class WeeklyPlanPublicModelTests
{
    [Fact]
    public void PublicPlanProjectionContainsNoPrivateDiagnosticOrPersonalFields()
    {
        Type[] projectionTypes =
        [
            typeof(WeeklyPlanView),
            typeof(WeeklyPlanRecommendationView),
            typeof(WeeklyPlanWeekView),
            typeof(WeeklyPlanWeekFocusView),
        ];
        string[] forbiddenProperties =
        [
            "ExpectedOptionId",
            "SelectedOptionId",
            "QuestionId",
            "QuestionPrompt",
            "DisplayNameOfUser",
            "ProfessionalGoal",
            "MasteryScore",
            "ExerciseId",
            "Solution",
        ];

        foreach (Type type in projectionTypes)
        {
            string[] properties = type.GetProperties().Select(property => property.Name).ToArray();
            Assert.Empty(properties.Intersect(forbiddenProperties, StringComparer.OrdinalIgnoreCase));
        }
    }
}
