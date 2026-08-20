using System;
using System.Collections.Generic;

public static class Submission
{
    // Le référentiel est fermé et ordonné : c'est lui qui décide des pièces exigées et de
    // l'ordre du verdict, jamais l'ordre d'assemblage du dossier.
    private static readonly string[] RequiredEvidences = ["tests", "security-review", "rollback-plan"];

    public static string MilestoneVerdict(string evidences)
    {
        if (string.IsNullOrWhiteSpace(evidences))
        {
            throw new ArgumentException("Un dossier vide ne s'audite pas.", nameof(evidences));
        }

        var dossier = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string piece in evidences.Split(';'))
        {
            string[] parts = piece.Split('=');
            bool readable = parts.Length == 2
                && Array.IndexOf(RequiredEvidences, parts[0]) >= 0
                && parts[1] is "fresh" or "stale" or "missing";
            if (!readable)
            {
                throw new ArgumentException(
                    "Une pièce du dossier est illisible ou hors référentiel.", nameof(evidences));
            }

            if (!dossier.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Une pièce du dossier est fournie deux fois.", nameof(evidences));
            }
        }

        var offending = new List<string>();
        foreach (string required in RequiredEvidences)
        {
            // Une pièce exigée absente du dossier compte comme manquante : le silence ne prouve rien.
            string state = dossier.TryGetValue(required, out string? declared) ? declared : "missing";

            // La preuve périmée décrit un autre code que celui qui part : elle bloque sous son nom,
            // parce que sa correction — rejouer — diffère de celle de l'absente — produire.
            if (state != "fresh")
            {
                offending.Add(required + "=" + state);
            }
        }

        return offending.Count == 0 ? "ready" : "blocked|" + string.Join(';', offending);
    }
}
