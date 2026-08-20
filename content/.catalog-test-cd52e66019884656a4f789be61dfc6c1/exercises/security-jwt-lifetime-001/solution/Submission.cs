using System;
using System.Text.Json;

public static class Submission
{
    public static bool IsWithinLifetime(string token, int nowUnixSeconds, int toleranceSeconds)
    {
        if (string.IsNullOrEmpty(token))
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
            if (payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // L'expiration est obligatoire : sans elle, le jeton ne mourrait jamais.
            if (!payload.RootElement.TryGetProperty("exp", out JsonElement expiration)
                || expiration.ValueKind != JsonValueKind.Number
                || !expiration.TryGetInt64(out long expirationSeconds))
            {
                return false;
            }

            // Arithmétique en 64 bits : la somme avec la tolérance peut déborder un int.
            long now = nowUnixSeconds;
            long tolerance = toleranceSeconds;

            // Borne stricte : à l'instant exact de l'expiration tolérée, c'est déjà refusé.
            if (now >= expirationSeconds + tolerance)
            {
                return false;
            }

            // La prise d'effet est facultative ; présente, elle doit être numérique et atteinte.
            if (payload.RootElement.TryGetProperty("nbf", out JsonElement notBefore))
            {
                if (notBefore.ValueKind != JsonValueKind.Number
                    || !notBefore.TryGetInt64(out long notBeforeSeconds))
                {
                    return false;
                }

                if (now < notBeforeSeconds - tolerance)
                {
                    return false;
                }
            }

            return true;
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
            throw new FormatException("Charge utile tronquée.");
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
