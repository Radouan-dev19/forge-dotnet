using System.Text;

namespace ForgeDotNet.Domain.SqlLab;

public static class SqlStatementGuard
{
    private static readonly HashSet<string> AllowedFirstTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT",
        "INSERT",
        "UPDATE",
        "DELETE",
        "WITH",
        "WAITFOR",
    };

    private static readonly HashSet<string> ForbiddenTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALTER",
        "BACKUP",
        "BULK",
        "CREATE",
        "DBCC",
        "DENY",
        "DROP",
        "EXEC",
        "EXECUTE",
        "EXTERNAL",
        "GRANT",
        "KILL",
        "LOGIN",
        "OPENQUERY",
        "OPENROWSET",
        "OPENDATASOURCE",
        "RECONFIGURE",
        "RESTORE",
        "REVOKE",
        "SHUTDOWN",
        "TRUNCATE",
        "USE",
        "XP_CMDSHELL",
    };

    public static IReadOnlyList<string> Validate(string? query, int maximumCharacters)
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(query))
        {
            issues.Add("La requête SQL est obligatoire.");
            return issues;
        }

        if (maximumCharacters <= 0 || query.Length > maximumCharacters)
        {
            issues.Add($"La requête dépasse la limite de {maximumCharacters} caractères.");
            return issues;
        }

        TokenizationResult tokenization = Tokenize(query);
        if (!tokenization.IsComplete)
        {
            issues.Add("La requête contient une chaîne, un identifiant ou un commentaire non terminé.");
            return issues;
        }

        if (tokenization.Tokens.Count == 0)
        {
            issues.Add("La requête SQL est obligatoire.");
            return issues;
        }

        if (!AllowedFirstTokens.Contains(tokenization.Tokens[0]))
        {
            issues.Add("Seules les requêtes SELECT, WITH, INSERT, UPDATE, DELETE et le test WAITFOR sont autorisés dans ce laboratoire.");
        }

        string? forbidden = tokenization.Tokens.FirstOrDefault(ForbiddenTokens.Contains);
        if (forbidden is not null)
        {
            issues.Add($"L’instruction {forbidden.ToUpperInvariant()} n’est pas autorisée dans une session de laboratoire.");
        }

        if (tokenization.StatementSeparators > 1
            || tokenization.StatementSeparators == 1 && !tokenization.EndsWithSeparator)
        {
            issues.Add("Une seule instruction SQL est autorisée par exécution.");
        }

        if (ContainsThreePartName(tokenization.Tokens))
        {
            issues.Add("Les références inter-base ne sont pas autorisées.");
        }

        return issues.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool ContainsThreePartName(IReadOnlyList<string> tokens)
    {
        for (int index = 0; index + 4 < tokens.Count; index++)
        {
            if (IsIdentifier(tokens[index])
                && tokens[index + 1] == "."
                && IsIdentifier(tokens[index + 2])
                && tokens[index + 3] == "."
                && IsIdentifier(tokens[index + 4]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIdentifier(string token) => token != "." && token != ";";

    private static TokenizationResult Tokenize(string query)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        int separators = 0;
        bool endsWithSeparator = false;
        int index = 0;

        void Flush()
        {
            if (current.Length == 0)
            {
                return;
            }

            tokens.Add(current.ToString());
            current.Clear();
        }

        while (index < query.Length)
        {
            char character = query[index];
            if (char.IsWhiteSpace(character))
            {
                Flush();
                index++;
                continue;
            }

            if (character == '-' && index + 1 < query.Length && query[index + 1] == '-')
            {
                Flush();
                index += 2;
                while (index < query.Length && query[index] is not '\r' and not '\n')
                {
                    index++;
                }

                continue;
            }

            if (character == '/' && index + 1 < query.Length && query[index + 1] == '*')
            {
                Flush();
                int end = query.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    return new TokenizationResult(tokens, separators, false, false);
                }

                index = end + 2;
                continue;
            }

            if (character == '\'' || character == '"' || character == '[')
            {
                Flush();
                char closing = character == '[' ? ']' : character;
                var identifier = new StringBuilder();
                index++;
                bool closed = false;
                while (index < query.Length)
                {
                    char value = query[index++];
                    if (value == closing)
                    {
                        if (index < query.Length && query[index] == closing)
                        {
                            identifier.Append(closing);
                            index++;
                            continue;
                        }

                        closed = true;
                        break;
                    }

                    identifier.Append(value);
                }

                if (!closed)
                {
                    return new TokenizationResult(tokens, separators, false, false);
                }

                if (character == '[' || character == '"')
                {
                    tokens.Add(identifier.ToString());
                }

                continue;
            }

            if (character is '.' or ';')
            {
                Flush();
                tokens.Add(character.ToString());
                if (character == ';')
                {
                    separators++;
                    endsWithSeparator = query[(index + 1)..].Trim().Length == 0;
                }

                index++;
                continue;
            }

            if (char.IsLetterOrDigit(character) || character is '_' or '#' or '@')
            {
                current.Append(character);
            }
            else
            {
                Flush();
            }

            index++;
        }

        Flush();
        return new TokenizationResult(tokens, separators, endsWithSeparator, true);
    }

    private sealed record TokenizationResult(
        IReadOnlyList<string> Tokens,
        int StatementSeparators,
        bool EndsWithSeparator,
        bool IsComplete);
}
