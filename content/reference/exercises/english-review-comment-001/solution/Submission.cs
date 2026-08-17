using System;
using System.Collections.Generic;

public static class Submission
{
    private const string Unlabelled = "unlabelled";

    // La convention d'étiquetage, et rien d'autre. L'ajouter à cette table est le seul geste
    // nécessaire pour reconnaître une quatrième étiquette.
    private static readonly Dictionary<string, string> Conventions = new(StringComparer.Ordinal)
    {
        ["must"] = "blocking",
        ["nit"] = "suggestion",
        ["q"] = "question",
    };

    public static string CommentKind(string comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        int separator = comment.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return Unlabelled;
        }

        string label = comment[..separator].Trim().ToLowerInvariant();

        // Le défaut est délibérément le plus faible : une étiquette inconnue ne doit jamais gagner
        // le pouvoir d'arrêter une fusion.
        return Conventions.GetValueOrDefault(label, Unlabelled);
    }
}
