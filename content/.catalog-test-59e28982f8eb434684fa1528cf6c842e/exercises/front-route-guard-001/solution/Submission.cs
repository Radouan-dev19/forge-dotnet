using System;
using System.Text.Json;

public static class Submission
{
    public static string GuardDecision(string token, string requiredScope, int nowUnix, string currentPath)
    {
        // Le droit exige et le chemin sont des donnees de la route : leur absence est une erreur.
        if (requiredScope is null)
        {
            throw new ArgumentNullException(nameof(requiredScope));
        }

        if (currentPath is null)
        {
            throw new ArgumentNullException(nameof(currentPath));
        }

        // Issue commune a toutes les situations non authentifiees : on garde le chemin de retour.
        string redirect = "redirect:login?return=" + currentPath;

        // Un jeton absent decrit un utilisateur non authentifie.
        if (token is null)
        {
            return redirect;
        }

        // Un JWT porte exactement trois segments separes par un point.
        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return redirect;
        }

        int expiry;
        string scope;
        try
        {
            // base64url vers base64 standard : caracteres modifies puis remplissage retabli.
            string base64 = segments[1].Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            byte[] payload = Convert.FromBase64String(base64);
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;

            // Sans revendication exp numerique, le jeton est inexploitable : on redirige.
            if (!root.TryGetProperty("exp", out JsonElement expElement)
                || expElement.ValueKind != JsonValueKind.Number)
            {
                return redirect;
            }

            expiry = expElement.GetInt32();

            // scope absent est traite comme une liste vide de droits.
            scope = root.TryGetProperty("scope", out JsonElement scopeElement)
                && scopeElement.ValueKind == JsonValueKind.String
                    ? scopeElement.GetString()!
                    : "";
        }
        catch (FormatException)
        {
            // base64url invalide : porteur non authentifiable.
            return redirect;
        }
        catch (JsonException)
        {
            // Charge utile qui n'est pas du JSON valide : meme issue.
            return redirect;
        }

        // Expiration inclusive : un jeton echu a l'instant courant est deja expire.
        if (expiry <= nowUnix)
        {
            return redirect;
        }

        // Les droits se separent sur l'espace ; le droit exige doit y figurer a l'identique.
        string[] grantedScopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Array.IndexOf(grantedScopes, requiredScope) >= 0 ? "allow" : "forbidden";
    }
}
