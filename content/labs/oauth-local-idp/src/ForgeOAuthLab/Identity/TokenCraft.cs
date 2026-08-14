using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ForgeOAuthLab.Identity;

/// <summary>
/// Fabrique et lit les jetons du guichet local : trois segments Base64Url signés HMAC-SHA256,
/// entièrement en bibliothèque standard — la mécanique des exercices, sans dépendance.
/// </summary>
public static class TokenCraft
{
    /// <summary>Clé factice du guichet local. Jamais une valeur réelle dans le dépôt.</summary>
    public const string SigningKey = "forge-fake-local-idp-signing-key-0001";

    public const string Issuer = "forge-local-idp";

    /// <summary>Audience des jetons d'accès : l'API de ressource, jamais le client.</summary>
    public const string ResourceAudience = "forge-oauth-api";

    public static string IssueAccessToken(string subject, string scope, long nowUnixSeconds)
    {
        string payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["iss"] = Issuer,
            ["aud"] = ResourceAudience,
            ["sub"] = subject,
            ["scope"] = scope,
            ["iat"] = nowUnixSeconds,
            ["exp"] = nowUnixSeconds + 300,
        });

        return Sign(payload);
    }

    public static string IssueIdToken(
        string subject,
        string clientId,
        string nonce,
        string accessToken,
        long nowUnixSeconds)
    {
        string payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["iss"] = Issuer,
            // L'audience du jeton d'identité est le CLIENT : c'est lui le destinataire.
            ["aud"] = clientId,
            ["sub"] = subject,
            ["nonce"] = nonce,
            ["azp"] = clientId,
            ["at_hash"] = ComputeAtHash(accessToken),
            ["iat"] = nowUnixSeconds,
            ["exp"] = nowUnixSeconds + 300,
        });

        return Sign(payload);
    }

    /// <summary>Moitié gauche du condensat du jeton d'accès, encodée en Base64Url.</summary>
    public static string ComputeAtHash(string accessToken)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(accessToken));
        return ToBase64Url(hash[..16]);
    }

    public static bool TryReadPayload(string token, out JsonDocument payload)
    {
        payload = null!;
        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            return false;
        }

        try
        {
            payload = JsonDocument.Parse(FromBase64Url(segments[1]));
            return payload.RootElement.ValueKind == JsonValueKind.Object;
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

    private static string Sign(string payloadJson)
    {
        string header = ToBase64Url(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""));
        string payload = ToBase64Url(Encoding.UTF8.GetBytes(payloadJson));
        byte[] signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(SigningKey),
            Encoding.UTF8.GetBytes($"{header}.{payload}"));
        return $"{header}.{payload}.{ToBase64Url(signature)}";
    }

    public static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] FromBase64Url(string segment)
    {
        string base64 = segment.Replace('-', '+').Replace('_', '/');
        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            throw new FormatException("Segment tronqué.");
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
