using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

internal static class ContentFileClassifier
{
    public static bool IsIgnoredJson(string relativePath)
    {
        string normalized = Normalize(relativePath);
        return normalized.StartsWith("schemas/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("authoring/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/starter/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/solution/", StringComparison.OrdinalIgnoreCase);
    }

    public static ContentDocumentType? Classify(string relativePath)
    {
        string normalized = $"/{Normalize(relativePath)}";
        string fileName = Path.GetFileName(normalized);

        if (fileName.Equals("lesson.json", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.Lesson;
        }

        if (fileName.Equals("exercise.json", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.Exercise;
        }

        if (fileName.Equals("scenario.json", StringComparison.OrdinalIgnoreCase))
        {
            if (normalized.Contains("/debugging/", StringComparison.OrdinalIgnoreCase))
            {
                return ContentDocumentType.DebugScenario;
            }

            if (normalized.Contains("/sql/", StringComparison.OrdinalIgnoreCase))
            {
                return ContentDocumentType.SqlScenario;
            }
        }

        if (normalized.Contains("/curriculum/", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains("/lessons/", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.Curriculum;
        }

        if (normalized.Contains("/interviews/", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.InterviewQuestion;
        }

        if (normalized.Contains("/english/", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.EnglishActivity;
        }

        if (normalized.Contains("/projects/", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.Project;
        }

        if (normalized.Contains("/reviews/", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.ReviewCardBank;
        }

        return null;
    }

    public static string SchemaFileName(ContentDocumentType documentType) => documentType switch
    {
        ContentDocumentType.Lesson => "lesson.schema.json",
        ContentDocumentType.Exercise => "exercise.schema.json",
        ContentDocumentType.Curriculum => "curriculum.schema.json",
        ContentDocumentType.DebugScenario => "debug.schema.json",
        ContentDocumentType.SqlScenario => "sql.schema.json",
        ContentDocumentType.InterviewQuestion => "interview.schema.json",
        ContentDocumentType.EnglishActivity => "english.schema.json",
        ContentDocumentType.Project => "project.schema.json",
        ContentDocumentType.ReviewCardBank => "review.schema.json",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
    };

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
