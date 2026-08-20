using System;
using System.Collections.Generic;

public static class Submission
{
    public static string HostingRecommendation(string workload)
    {
        if (string.IsNullOrWhiteSpace(workload))
        {
            throw new ArgumentException("Un profil vide ne se recommande pas.", nameof(workload));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in workload.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2 || !attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut du profil est illisible ou répété.", nameof(workload));
            }
        }

        if (attributes.Count != 3)
        {
            throw new ArgumentException("Le profil se décrit par exactement trois attributs.", nameof(workload));
        }

        string artifact = ValueOf(attributes, "artifact", "code", "container");
        string scale = ValueOf(attributes, "scale", "steady", "bursty", "event-driven");
        string delivery = ValueOf(attributes, "delivery", "single-revision", "multi-revision");

        // Le rythme fixe le modèle de facturation, la conséquence la plus dure à corriger : une
        // charge qui dort ne paie pas d'instance permanente.
        if (scale == "event-driven")
        {
            return artifact == "code" ? "functions|per-event-billing" : "container-apps|scale-to-zero";
        }

        // Pour les rythmes continus, la livraison départage : révisions à trafic réparti pour un
        // conteneur, emplacements d'échange pour du code, exécution gérée sinon.
        return (artifact, delivery) switch
        {
            ("container", "multi-revision") => "container-apps|revision-traffic",
            ("container", "single-revision") => "app-service|single-container",
            ("code", "multi-revision") => "app-service|deployment-slots",
            _ => "app-service|managed-runtime",
        };
    }

    private static string ValueOf(Dictionary<string, string> attributes, string key, params string[] allowed)
    {
        if (!attributes.TryGetValue(key, out string? value) || Array.IndexOf(allowed, value) < 0)
        {
            throw new ArgumentException("Un attribut du profil manque ou sort du vocabulaire.", nameof(attributes));
        }

        return value;
    }
}
