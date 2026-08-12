using System.Text.RegularExpressions;

namespace ForgeDotNet.Infrastructure.Content;

/// <summary>
/// Isole la prose d'un document Markdown, blocs clôturés et segments en accents graves retirés.
/// </summary>
/// <remarks>
/// Les règles de contenu portent sur ce qu'un apprenant lit, pas sur le code qu'on lui montre.
/// Sans cette distinction, <c>IReadOnlyList&lt;T&gt;</c> serait pris pour une balise HTML et
/// <c>$"…"</c> pour un marqueur de génération : un cours C# deviendrait impossible à écrire.
/// La sécurité ne repose pas sur cette analyse — le lecteur produit un modèle typé sans passe-plat
/// HTML, et Blazor échappe tout texte rendu — mais la prose reste contrôlée en défense en profondeur.
/// </remarks>
internal static partial class MarkdownProse
{
    public static string Extract(string markdown) =>
        InlineCodeRegex().Replace(CodeFenceRegex().Replace(markdown, "\n\n"), " ");

    public static bool ContainsCodeFence(string markdown) => CodeFenceRegex().IsMatch(markdown);

    [GeneratedRegex(@"^[ \t]*```.*?^[ \t]*```[ \t]*$",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        1000)]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"`[^`\n]*`", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex InlineCodeRegex();
}
