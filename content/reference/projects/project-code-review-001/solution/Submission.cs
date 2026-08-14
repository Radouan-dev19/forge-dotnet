using System;
using System.Collections.Generic;

public static class Submission
{
    // Le classement vit dans une table, pas dans une cascade de branches fragiles.
    // Corriger un verdict se fait a un seul endroit, et la table se relit d'un coup d'oeil.
    private static readonly Dictionary<string, string> Catalog = new(StringComparer.Ordinal)
    {
        // Defauts de correction : ils produisent un resultat faux.
        ["missing-null-check"] = "blocking:correctness",
        ["off-by-one"] = "blocking:correctness",

        // Defauts de securite : ils ouvrent une breche.
        ["sql-injection"] = "blocking:security",
        ["hardcoded-secret"] = "blocking:security",

        // Defauts de concurrence : ils cassent sous acces concurrent.
        ["unsynchronized-list-access"] = "blocking:concurrency",
        ["double-checked-locking-broken"] = "blocking:concurrency",

        // Remarques cosmetiques : elles ne bloquent jamais une fusion.
        ["variable-naming-nit"] = "minor:style",
        ["extra-blank-line"] = "minor:style",
    };

    public static string ClassifyFinding(string findingId)
    {
        // Un identifiant absent est un appel fautif, jamais un simple inconnu.
        ArgumentNullException.ThrowIfNull(findingId);

        // Un identifiant du catalogue rend son verdict connu.
        if (Catalog.TryGetValue(findingId, out string? classification))
        {
            return classification;
        }

        // Tout identifiant hors catalogue est signale comme inconnu, sans lever d'exception.
        return "unknown";
    }
}
