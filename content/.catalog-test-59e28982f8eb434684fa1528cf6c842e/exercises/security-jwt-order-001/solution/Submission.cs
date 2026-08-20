using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class Submission
{
    public static string FirstRejection(
        string token,
        string secret,
        string expectedIssuer,
        string expectedAudience,
        int nowUnixSeconds)
    {
        if (string.IsNullOrEmpty(token) || secret is null)
        {
            return "format";
        }

        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return "format";
        }

        // Étape 1 — format : les trois segments se décodent, en-tête et charge utile sont du JSON.
        JsonDocument? header = null;
        JsonDocument? payload = null;
        try
        {
            try
            {
                header = JsonDocument.Parse(DecodeBase64Url(segments[0]));
                payload = JsonDocument.Parse(DecodeBase64Url(segments[1]));
                _ = DecodeBase64Url(segments[2]);
            }
            catch (FormatException)
            {
                return "format";
            }
            catch (JsonException)
            {
                return "format";
            }

            if (header.RootElement.ValueKind != JsonValueKind.Object
                || payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "format";
            }

            // Étape 2 — algorithme : l'annonce doit être exactement celle qu'impose le serveur.
            if (!header.RootElement.TryGetProperty("alg", out JsonElement algorithm)
                || algorithm.ValueKind != JsonValueKind.String
                || !string.Equals(algorithm.GetString(), "HS256", StringComparison.Ordinal))
            {
                return "algorithm";
            }

            // Étape 3 — signature : recalcul HMAC et comparaison en temps constant.
            byte[] signedBytes = Encoding.UTF8.GetBytes(segments[0] + "." + segments[1]);
            byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signedBytes);
            byte[] presented = DecodeBase64Url(segments[2]);
            if (!CryptographicOperations.FixedTimeEquals(expected, presented))
            {
                return "signature";
            }

            // À partir d'ici seulement, les revendications ont une valeur probante.

            // Étape 4 — expiration stricte, sans tolérance : elle est exigée et numérique.
            if (!payload.RootElement.TryGetProperty("exp", out JsonElement expiration)
                || expiration.ValueKind != JsonValueKind.Number
                || !expiration.TryGetInt64(out long expirationSeconds)
                || nowUnixSeconds >= expirationSeconds)
            {
                return "expiration";
            }

            // Étape 5 — émetteur.
            if (!payload.RootElement.TryGetProperty("iss", out JsonElement issuer)
                || issuer.ValueKind != JsonValueKind.String
                || !string.Equals(issuer.GetString(), expectedIssuer, StringComparison.Ordinal))
            {
                return "issuer";
            }

            // Étape 6 — audience, sous ses deux formes légitimes.
            if (!payload.RootElement.TryGetProperty("aud", out JsonElement audience)
                || !MatchesAudience(audience, expectedAudience))
            {
                return "audience";
            }

            return "valid";
        }
        finally
        {
            header?.Dispose();
            payload?.Dispose();
        }
    }

    private static bool MatchesAudience(JsonElement audience, string expectedAudience)
    {
        if (audience.ValueKind == JsonValueKind.String)
        {
            return string.Equals(audience.GetString(), expectedAudience, StringComparison.Ordinal);
        }

        if (audience.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

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

    private static byte[] DecodeBase64Url(string segment)
    {
        string base64 = segment.Replace('-', '+').Replace('_', '/');

        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Segment tronqué : reste impossible.");
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
