using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ResolveAllowedOrigin(string requestOrigin, string allowlist, bool withCredentials)
    {
        var allowed = new HashSet<string>(
            (allowlist ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.Ordinal);

        // Joker légitime uniquement pour une ressource publique sans identifiants.
        if (allowed.Contains("*") && !withCredentials)
        {
            return "*";
        }

        // Sinon, l'origine reçue n'est autorisée qu'après confrontation à la liste :
        // l'écho validé est sûr, l'écho aveugle serait le joker interdit déguisé.
        if (!string.IsNullOrEmpty(requestOrigin) && allowed.Contains(requestOrigin))
        {
            return requestOrigin;
        }

        // Aucune origine autorisée : le navigateur bloquera la lecture de la réponse.
        return "";
    }
}
