using System;
using System.Collections.Generic;

public static class Submission
{
    public static string LoginResponse(string outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Une tentative vide ne se répond pas.", nameof(outcome));
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in outcome.Split(';'))
        {
            string[] parts = pair.Split('=');
            if (parts.Length != 2 || !attributes.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut de la tentative est illisible ou répété.", nameof(outcome));
            }
        }

        if (attributes.Count != 3)
        {
            throw new ArgumentException("La tentative se décrit par exactement trois attributs.", nameof(outcome));
        }

        string account = ValueOf(attributes, "account", "known", "unknown");
        string password = ValueOf(attributes, "password", "correct", "wrong", "expired");
        string state = ValueOf(attributes, "state", "active", "locked");

        // Tout ce qui précède la preuve du mot de passe partage la même face publique : chaque
        // nuance de message serait un bit offert à l'énumération de comptes.
        if (account == "unknown")
        {
            return "invalid-credentials|unknown-account";
        }

        // Le verrou prime sur la validité : seul un compte existant se verrouille, et le dire
        // publiquement confirmerait son existence.
        if (state == "locked")
        {
            return "invalid-credentials|locked-account";
        }

        if (password == "wrong")
        {
            return "invalid-credentials|wrong-password";
        }

        // Seule cause nommée publiquement : l'appelant a prouvé le mot de passe, il est titulaire.
        if (password == "expired")
        {
            return "password-expired|expired-password";
        }

        return "success|success";
    }

    private static string ValueOf(Dictionary<string, string> attributes, string key, params string[] allowed)
    {
        if (!attributes.TryGetValue(key, out string? value) || Array.IndexOf(allowed, value) < 0)
        {
            throw new ArgumentException(
                "Un attribut de la tentative manque ou sort du vocabulaire.", nameof(attributes));
        }

        return value;
    }
}
