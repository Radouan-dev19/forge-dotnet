using System;
using System.Collections.Generic;

public static class Submission
{
    public static string RecommendedLifetime(string dependency)
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            throw new ArgumentException("Un profil vide ne se recommande pas.", nameof(dependency));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in dependency.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2 || !attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut du profil est illisible ou répété.", nameof(dependency));
            }
        }

        if (attributes.Count != 3)
        {
            throw new ArgumentException("Le profil se décrit par exactement trois attributs.", nameof(dependency));
        }

        string state = ValueOf(attributes, "state", "none", "per-request", "shared-mutable");
        string cost = ValueOf(attributes, "cost", "cheap", "expensive");
        string usesScoped = ValueOf(attributes, "uses-scoped", "yes", "no");

        // Aucun choix de durée ne réconcilie un état partagé avec un service recréé à chaque
        // requête : ce profil décrit un service à découper, pas à enregistrer.
        if (state == "shared-mutable" && usesScoped == "yes")
        {
            return "conflict|captive-dependency";
        }

        // L'état prime sur le coût : partager un état de requête mélange les utilisateurs.
        if (state == "per-request")
        {
            return "scoped|request-state";
        }

        if (state == "shared-mutable")
        {
            return "singleton|shared-state";
        }

        // Le consommateur d'un service de requête ne vit jamais plus longtemps que lui, sans quoi
        // il fige la première instance reçue — la dépendance captive.
        if (usesScoped == "yes")
        {
            return "scoped|scoped-dependency";
        }

        // Le coût de construction ne départage que ce que rien d'autre n'a classé.
        return cost == "expensive" ? "singleton|construction-cost" : "transient|stateless-cheap";
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
