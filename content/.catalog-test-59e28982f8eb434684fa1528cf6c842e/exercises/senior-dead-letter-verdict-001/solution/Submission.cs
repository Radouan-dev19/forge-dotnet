using System;
using System.Collections.Generic;

public static class Submission
{
    public static string PoisonVerdict(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Un message vide ne se route pas.", nameof(message));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in message.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2 || !attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut du message est illisible ou répété.", nameof(message));
            }
        }

        if (attributes.Count != 4
            || !attributes.TryGetValue("payload", out string? payload) || payload is not ("ok" or "malformed")
            || !attributes.TryGetValue("error", out string? error)
            || error is not ("none" or "transient" or "permanent")
            || !attributes.TryGetValue("attempts", out string? rawAttempts)
            || !int.TryParse(rawAttempts, out int attempts) || attempts < 1
            || !attributes.TryGetValue("max", out string? rawMax)
            || !int.TryParse(rawMax, out int max) || max < 1 || attempts > max)
        {
            throw new ArgumentException("Le relevé du message ne décrit aucun message réel.", nameof(message));
        }

        // Le message illisible sort en premier, budget intact : le rejouer reproduit l'échec à
        // l'identique et fabrique la boucle empoisonnée devant les messages sains.
        if (payload == "malformed")
        {
            return "dead-letter|malformed-payload";
        }

        if (error == "none")
        {
            return "ack|processed";
        }

        // Une règle métier violée le restera à la millième tentative : router immédiatement date
        // correctement le problème et économise les faux réveils.
        if (error == "permanent")
        {
            return "dead-letter|non-retryable";
        }

        // Budget strict : la tentative qui atteint le maximum est la dernière.
        return attempts < max ? "requeue|budget-remaining" : "dead-letter|budget-exhausted";
    }
}
