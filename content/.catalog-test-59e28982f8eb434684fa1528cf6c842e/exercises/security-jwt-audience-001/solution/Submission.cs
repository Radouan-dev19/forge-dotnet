using System;
using System.Text.Json;

public static class Submission
{
    public static bool IsForAudience(string token, string expectedAudience)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expectedAudience))
        {
            return false;
        }

        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return false;
        }

        try
        {
            using JsonDocument payload = JsonDocument.Parse(DecodeBase64Url(segments[1]));
            if (payload.RootElement.ValueKind != JsonValueKind.Object
                || !payload.RootElement.TryGetProperty("aud", out JsonElement audience))
            {
                // Sans destinataire déclaré, le jeton n'est destiné à personne.
                return false;
            }

            // Forme chaîne : un seul destinataire, égalité stricte.
            if (audience.ValueKind == JsonValueKind.String)
            {
                return string.Equals(audience.GetString(), expectedAudience, StringComparison.Ordinal);
            }

            // Forme tableau : plusieurs destinataires, les éléments non textuels s'ignorent.
            if (audience.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in audience.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String
                        && string.Equals(item.GetString(), expectedAudience, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            // Nombre, objet, booléen : l'émetteur sort du contrat, on ne devine pas.
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        string base64 = segment.Replace('-', '+').Replace('_', '/');

        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Segment de charge utile tronqué.");
        }

        if (remainder == 2)
        {
            base64 += "==";
        }
        else if (remainder == 3)
        {
            base64 += "=";
        }

        return Convert.FromBase64String(base64);
    }
}
