using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

internal static class ContentFileClassifier
{
    /// <summary>
    /// Artefacts JSON qu'un contenu publie sans qu'ils soient des manifestes Forge.
    /// </summary>
    /// <remarks>
    /// Le contrat OpenAPI d'un laboratoire est un livrable montré à l'apprenant, pas un manifeste :
    /// il ne porte ni identifiant, ni version de schéma Forge, et aucun schéma ne pourrait le valider.
    /// Sans cette exception, valider <c>content/labs</c> échouerait sur <c>unknown-manifest</c>, ce qui
    /// interdirait de rattacher les laboratoires au produit. La configuration locale d'un laboratoire
    /// relève du même statut : c'est un fichier que l'apprenant ouvre et modifie, pas un manifeste.
    /// Les laboratoires front-end JavaScript publient leur configuration d'outillage — manifeste npm,
    /// verrou de dépendances, configuration TypeScript et Angular — qui n'est pas davantage un
    /// manifeste Forge, mais que l'apprenant restaure et exécute.
    /// </remarks>
    private static readonly string[] PublishedArtefacts =
    [
        "openapi.json", "appsettings.json",
        "package.json", "package-lock.json", "tsconfig.json", "tsconfig.spec.json", "angular.json",
    ];

    public static bool IsIgnoredJson(string relativePath)
    {
        string normalized = Normalize(relativePath);
        return normalized.StartsWith("schemas/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("authoring/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/starter/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/solution/", StringComparison.OrdinalIgnoreCase)
            // Compiler un laboratoire sur place dépose des artefacts de build (*.deps.json,
            // project.assets.json) sous bin/ et obj/ : ce sont des sorties d'outillage, jamais
            // des manifestes, et les commandes des laboratoires invitent précisément à compiler.
            || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || PublishedArtefacts.Contains(Path.GetFileName(normalized), StringComparer.OrdinalIgnoreCase);
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

        if (fileName.Equals("lab.json", StringComparison.OrdinalIgnoreCase))
        {
            return ContentDocumentType.Lab;
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
        ContentDocumentType.Lab => "lab.schema.json",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
    };

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
