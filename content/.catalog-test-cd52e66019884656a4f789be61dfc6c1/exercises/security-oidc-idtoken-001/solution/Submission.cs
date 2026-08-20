using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class Submission
{
    public static string IdTokenVerdict(
        string idToken,
        string expectedNonce,
        string expectedClientId,
        string accessToken)
    {
        if (string.IsNullOrEmpty(idToken) || accessToken is null)
        {
            return "format";
        }

        string[] segments = idToken.Split('.');
        if (segments.Length != 3)
        {
            return "format";
        }

        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(DecodeBase64Url(segments[1]));
        }
        catch (FormatException)
        {
            return "format";
        }
        catch (JsonException)
        {
            return "format";
        }

        using (payload)
        {
            if (payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "format";
            }

            // Lien numéro un : la demande. Le nonce gravé doit être celui du client.
            if (!TryReadString(payload.RootElement, "nonce", out string nonce)
                || !string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
            {
                return "nonce";
            }

            // Lien numéro deux : le client. La partie autorisée nomme le destinataire.
            if (!TryReadString(payload.RootElement, "azp", out string authorizedParty)
                || !string.Equals(authorizedParty, expectedClientId, StringComparison.Ordinal))
            {
                return "azp";
            }

            // Lien numéro trois : le jeton d'accès reçu ensemble, scellé par empreinte.
            if (!TryReadString(payload.RootElement, "at_hash", out string atHash)
                || !string.Equals(atHash, ComputeAtHash(accessToken), StringComparison.Ordinal))
            {
                return "at-hash";
            }

            return "valid";
        }
    }

    private static string ComputeAtHash(string accessToken)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(accessToken));

        // La norme tronque à la moitié gauche : seize octets, pas trente-deux.
        byte[] leftHalf = hash[..16];

        return Convert.ToBase64String(leftHalf)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static bool TryReadString(JsonElement root, string name, out string value)
    {
        if (root.TryGetProperty(name, out JsonElement element)
            && element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString()!;
            return true;
        }

        value = "";
        return false;
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        string base64 = segment.Replace('-', '+').Replace('_', '/');

        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Segment impossible à décoder.");
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
