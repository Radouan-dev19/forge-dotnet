using System.Text.Json;
using ForgeOAuthLab.Identity;

namespace ForgeOAuthLab.Client;

/// <summary>
/// Contrôles du CLIENT sur le jeton d'identité reçu : audience, nonce, partie autorisée,
/// empreinte du jeton d'accès. C'est ici qu'un jeton d'accès déguisé en identité échoue.
/// </summary>
public static class IdTokenInspector
{
    public static string Inspect(string idToken, string expectedNonce, string clientId, string accessToken)
    {
        if (!TokenCraft.TryReadPayload(idToken, out JsonDocument payload))
        {
            return "unreadable";
        }

        using (payload)
        {
            // L'audience du jeton d'identité est le client : un jeton d'accès, dont
            // l'audience est l'API, échoue dès ici — les deux jetons ne se confondent pas.
            if (!HasStringClaim(payload, "aud", clientId))
            {
                return "wrong-audience";
            }

            if (!HasStringClaim(payload, "nonce", expectedNonce))
            {
                return "nonce-mismatch";
            }

            if (!HasStringClaim(payload, "azp", clientId))
            {
                return "wrong-authorized-party";
            }

            if (!HasStringClaim(payload, "at_hash", TokenCraft.ComputeAtHash(accessToken)))
            {
                return "access-token-mismatch";
            }

            return "accepted";
        }
    }

    private static bool HasStringClaim(JsonDocument payload, string name, string expected) =>
        payload.RootElement.TryGetProperty(name, out JsonElement claim)
            && claim.ValueKind == JsonValueKind.String
            && string.Equals(claim.GetString(), expected, StringComparison.Ordinal);
}
