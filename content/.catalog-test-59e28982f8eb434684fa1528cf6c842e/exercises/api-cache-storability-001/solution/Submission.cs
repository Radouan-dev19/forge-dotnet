using System;
using System.Collections.Generic;

public static class Submission
{
    // Statuts qu'un cache peut légitimement garder : succès, absences durables, redirection
    // permanente. Les erreurs de serveur en sont exclues, transitoires par nature.
    private static readonly HashSet<int> StorableStatuses = new() { 200, 203, 204, 301, 404, 410 };

    public static bool IsStorable(string method, int statusCode)
    {
        string verb = (method ?? "").Trim().ToUpperInvariant();

        // Une méthode à effet ne se met jamais en cache : resservir sa réponse
        // laisserait croire qu'une action a eu lieu. Seules les lectures passent.
        if (verb is not ("GET" or "HEAD"))
        {
            return false;
        }

        // Sur une lecture, seuls les statuts stockables sont gardés.
        return StorableStatuses.Contains(statusCode);
    }
}
