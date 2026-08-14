using System.Security.Cryptography;
using System.Text;

namespace ForgeJwtLab.Tests;

/// <summary>
/// Fabrique des jetons de test à la main — trois segments Base64Url et un HMAC-SHA256 —
/// exactement comme les exercices de la semaine 14 l'apprennent. Aucune bibliothèque de
/// jetons : ce que la suite éprouve ne dépend d'aucun émetteur externe.
/// </summary>
internal static class TestTokenFactory
{
    public const string SigningKey = "forge-fake-signing-key-for-local-jwt-lab-only-0001";
    public const string Issuer = "forge-issuer";
    public const string Audience = "forge-api";

    public static string CreateToken(
        string scope,
        int lifetimeSeconds = 300,
        string? signingKey = null,
        string? audience = null)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiration = now + lifetimeSeconds;

        string header = """{"alg":"HS256","typ":"JWT"}""";
        string payload =
            $$"""{"iss":"{{Issuer}}","aud":"{{audience ?? Audience}}","sub":"user-test","scope":"{{scope}}","exp":{{expiration}},"iat":{{now}}}""";

        string encodedHeader = ToBase64Url(Encoding.UTF8.GetBytes(header));
        string encodedPayload = ToBase64Url(Encoding.UTF8.GetBytes(payload));
        byte[] signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(signingKey ?? SigningKey),
            Encoding.UTF8.GetBytes($"{encodedHeader}.{encodedPayload}"));

        return $"{encodedHeader}.{encodedPayload}.{ToBase64Url(signature)}";
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
