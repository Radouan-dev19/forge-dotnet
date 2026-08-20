using System;
using System.Collections.Generic;

public static class Submission
{
    public static string SecretChannel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Un profil vide ne se range dans aucun canal.", nameof(value));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in value.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2 || !attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut du profil est illisible ou répété.", nameof(value));
            }
        }

        if (attributes.Count != 3)
        {
            throw new ArgumentException("Le profil se décrit par exactement trois attributs.", nameof(value));
        }

        string sensitivity = ValueOf(attributes, "sensitivity", "public", "secret");
        string consumer = ValueOf(attributes, "consumer", "platform-hosted", "local-dev");
        string rotation = ValueOf(attributes, "rotation", "static", "rotated");

        // Le tri d'abord : monter du non-sensible dans un canal de secret banalise le canal.
        if (sensitivity == "public")
        {
            return "configuration|not-a-secret";
        }

        // L'identité attestée supprime le secret stocké au lieu de le déplacer : la rotation
        // devient un non-sujet quand il n'y a plus rien à faire tourner.
        if (consumer == "platform-hosted")
        {
            return "managed-identity|no-stored-credential";
        }

        // Le poste local se départage par la rotation : le magasin utilisateur ne se resynchronise
        // jamais, seule une source centrale suit une valeur qui tourne.
        return rotation == "rotated" ? "key-vault|central-rotation" : "user-secrets|out-of-git";
    }

    private static string ValueOf(Dictionary<string, string> attributes, string key, params string[] allowed)
    {
        if (!attributes.TryGetValue(key, out string? found) || Array.IndexOf(allowed, found) < 0)
        {
            throw new ArgumentException("Un attribut du profil manque ou sort du vocabulaire.", nameof(attributes));
        }

        return found;
    }
}
