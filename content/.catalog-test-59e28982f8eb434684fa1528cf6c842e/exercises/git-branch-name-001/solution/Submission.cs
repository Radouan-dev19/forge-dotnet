using System;
using System.Text;

public static class Submission
{
    public static string NormalizeBranchName(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var builder = new StringBuilder(raw.Length);
        foreach (char character in raw)
        {
            char lowered = char.ToLowerInvariant(character);
            if (IsKept(lowered))
            {
                builder.Append(lowered);
                continue;
            }

            // Tout le reste devient un séparateur, tiret compris : convertir plutôt que supprimer,
            // sinon « import csv » se replierait en un seul mot que personne ne relit. Le tiret
            // passe ici pour que deux tirets écrits à la suite se réduisent comme les autres.
            if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        string normalized = builder.ToString().Trim('-');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Le nom de branche ne contient rien d'utilisable.", nameof(raw));
        }

        return normalized;
    }

    private static bool IsKept(char character) =>
        char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '/';
}
