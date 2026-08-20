using System;
using System.Text.Json;

public static class Submission
{
    public static bool UsesRequiredAlgorithm(string token, string requiredAlgorithm)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(requiredAlgorithm))
        {
            return false;
        }

        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return false;
        }

        string announced;
        try
        {
            using JsonDocument header = JsonDocument.Parse(DecodeBase64Url(segments[0]));
            if (header.RootElement.ValueKind != JsonValueKind.Object
                || !header.RootElement.TryGetProperty("alg", out JsonElement algorithm)
                || algorithm.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            announced = algorithm.GetString()!;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }

        // Liste noire d'abord : none est refusé sous toutes ses casses, exigence ou pas.
        if (string.Equals(announced, "none", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Puis confrontation stricte à la décision du vérificateur, sensible à la casse.
        return string.Equals(announced, requiredAlgorithm, StringComparison.Ordinal);
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        string base64 = segment.Replace('-', '+').Replace('_', '/');

        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Segment d'en-tête tronqué.");
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
