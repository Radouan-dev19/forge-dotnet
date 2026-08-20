using System;
using System.Collections.Generic;

public static class Submission
{
    public static string IncidentDigest(string journal)
    {
        if (string.IsNullOrWhiteSpace(journal))
        {
            throw new ArgumentException("Un journal vide ne raconte aucun incident.", nameof(journal));
        }

        int previousMinute = -1;
        int start = -1;
        string? impact = null;
        int mitigation = -1;
        string? owner = null;

        foreach (string entry in journal.Split(';'))
        {
            string[] parts = entry.Split(':');
            bool readable = parts.Length == 3 && parts[2].Length > 0
                && int.TryParse(parts[0], out int minute) && minute >= 0
                && parts[1] is "alert" or "impact" or "action" or "mitigation" or "assignment" or "note";
            if (!readable)
            {
                throw new ArgumentException("Une entrée du journal est illisible.", nameof(journal));
            }

            // Des minutes qui reculent signalent un journal recomposé : aucune extraction
            // « première occurrence » n'y garde son sens.
            int current = int.Parse(parts[0]);
            if (current < previousMinute)
            {
                throw new ArgumentException("La chronologie du journal décroît.", nameof(journal));
            }

            previousMinute = current;

            // Le début est la première trace observable — alerte ou impact — jamais la première
            // réaction de l'équipe, flatteuse et fausse.
            if (start < 0 && parts[1] is "alert" or "impact")
            {
                start = current;
            }

            if (impact is null && parts[1] == "impact")
            {
                impact = parts[2];
            }

            if (mitigation < 0 && parts[1] == "mitigation")
            {
                mitigation = current;
            }

            if (owner is null && parts[1] == "assignment")
            {
                owner = parts[2];
            }
        }

        // Un brief aux champs vides aurait l'air complet : l'incomplétude se déclare et se nomme.
        var missing = new List<string>();
        if (impact is null)
        {
            missing.Add("impact");
        }

        if (owner is null)
        {
            missing.Add("next-owner");
        }

        if (missing.Count > 0)
        {
            return "incomplete|" + string.Join(';', missing);
        }

        string mitigated = mitigation < 0 ? "none" : mitigation.ToString();
        return "impact=" + impact + "|start=" + start + "|mitigation=" + mitigated + "|next=" + owner;
    }
}
