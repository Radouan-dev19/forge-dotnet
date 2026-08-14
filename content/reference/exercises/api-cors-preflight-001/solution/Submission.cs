using System;
using System.Collections.Generic;

public static class Submission
{
    public static bool IsPreflightAllowed(
        string requestedMethod,
        string allowedMethods,
        string requestedHeaders,
        string allowedHeaders)
    {
        var methods = Parse(allowedMethods);

        // La méthode demandée doit être autorisée : sinon, la vraie requête n'a pas lieu.
        if (!methods.Contains((requestedMethod ?? "").Trim()))
        {
            return false;
        }

        var allowedHeaderSet = Parse(allowedHeaders);

        // Chaque en-tête demandé doit être autorisé — l'inclusion va des demandés vers
        // les autorisés. Un seul manquant refuse le préflight entier.
        foreach (string header in Split(requestedHeaders))
        {
            if (!allowedHeaderSet.Contains(header))
            {
                return false;
            }
        }

        // Méthode confirmée et tous les en-têtes demandés autorisés — y compris aucun.
        return true;
    }

    // Noms de méthodes et d'en-têtes HTTP : comparaison insensible à la casse.
    private static HashSet<string> Parse(string list) =>
        new(Split(list), StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> Split(string list) =>
        (list ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
