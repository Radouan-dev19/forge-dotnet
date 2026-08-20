using System;
using System.Collections.Generic;

public static class Submission
{
    public static string StateVerdict(string pendingStates, string consumedStates, string returnedState)
    {
        // Sans state, la réponse ne se rattache à rien : verdict avant tout registre.
        if (string.IsNullOrWhiteSpace(returnedState))
        {
            return "missing";
        }

        string candidate = returnedState.Trim();
        HashSet<string> consumed = ParseRegistry(consumedStates);
        HashSet<string> pending = ParseRegistry(pendingStates);

        // Le rejeu prime : un state déjà servi reste un rejeu, même s'il traîne
        // encore dans les attentes — la lecture la plus défavorable gagne.
        if (consumed.Contains(candidate))
        {
            return "replayed";
        }

        if (pending.Contains(candidate))
        {
            return "accepted";
        }

        // Jamais émis par ce client : quelqu'un fabrique des retours.
        return "forged";
    }

    private static HashSet<string> ParseRegistry(string registry)
    {
        var entries = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(registry))
        {
            return entries;
        }

        var options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        foreach (string entry in registry.Split(',', options))
        {
            entries.Add(entry);
        }

        return entries;
    }
}
