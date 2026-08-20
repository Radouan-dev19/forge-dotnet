using System;
using System.Text.Json;

public static class Submission
{
    public static string ReadClaim(string token, string claim)
    {
        if (token is null || claim is null)
        {
            throw new ArgumentException("Le jeton et le nom de revendication sont requis.");
        }

        // Trois segments exactement : tout autre découpage est un jeton malformé.
        string[] segments = token.Split('.');
        if (segments.Length != 3)
        {
            throw new ArgumentException("Le jeton ne porte pas trois segments.");
        }

        byte[] payloadBytes = DecodeBase64Url(segments[1]);

        try
        {
            using JsonDocument payload = JsonDocument.Parse(payloadBytes);
            if (payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("La charge utile décodée n'est pas un objet JSON.");
            }

            if (!payload.RootElement.TryGetProperty(claim, out JsonElement value))
            {
                // Revendication absente : situation normale, distincte du jeton malformé.
                return string.Empty;
            }

            // Une chaîne rend sa valeur nue ; tout autre type rend son texte JSON tel quel.
            return value.ValueKind == JsonValueKind.String
                ? value.GetString()!
                : value.GetRawText();
        }
        catch (JsonException)
        {
            throw new ArgumentException("La charge utile décodée n'est pas du JSON lisible.");
        }
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        // L'alphabet Base64Url diffère du Base64 classique par deux caractères.
        string base64 = segment.Replace('-', '+').Replace('_', '/');

        // Le remplissage supprimé à l'émission se déduit de la longueur restante.
        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            // Aucun flux Base64 valide ne laisse un reste de un : segment tronqué.
            throw new ArgumentException("Segment Base64Url tronqué.");
        }

        if (remainder == 2)
        {
            base64 += "==";
        }
        else if (remainder == 3)
        {
            base64 += "=";
        }

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            // Le contrat de l'exercice signale tout jeton malformé par la même exception.
            throw new ArgumentException("Caractères hors de l'alphabet Base64Url.");
        }
    }
}
