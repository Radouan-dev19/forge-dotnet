namespace ForgeDotNet.Infrastructure.Content;

/// <summary>
/// Codes des règles qui refusent un contenu structurellement valide mais pédagogiquement creux.
/// </summary>
public static class ContentAuthenticityRules
{
    /// <summary>Marqueur de génération laissé tel quel dans le contenu publié.</summary>
    public const string Placeholder = "unsubstituted-placeholder";

    /// <summary>Paragraphes recopiés d'un document à l'autre au-delà du seuil autorisé.</summary>
    public const string ClonedContent = "cloned-content";

    /// <summary>Leçon dont l'explication répète l'intuition, ou qui ne montre aucun code.</summary>
    public const string HollowLesson = "hollow-lesson";

    public static readonly IReadOnlyList<string> All = [Placeholder, ClonedContent, HollowLesson];

    public static bool IsAuthenticityCode(string code) =>
        All.Contains(code, StringComparer.Ordinal);
}
