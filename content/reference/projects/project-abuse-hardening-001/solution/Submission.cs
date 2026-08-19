using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class Submission
{
    public static string SignatureVerdict(string payload, string secret, string signature)
    {
        byte[] digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));

        // Base64Url sans remplissage : une seule forme valide par condensat, sinon un même jeton
        // aurait plusieurs écritures acceptées et la comparaison stricte perdrait son sens.
        string expected = Convert.ToBase64String(digest).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] presentedBytes = Encoding.UTF8.GetBytes(signature);

        // FixedTimeEquals rend faux sur des longueurs différentes sans raccourci lisible au chrono.
        return CryptographicOperations.FixedTimeEquals(expectedBytes, presentedBytes) ? "authentique" : "refus";
    }

    public static string SafePath(string requested)
    {
        // Étape 1 : décoder les séquences pour-cent exactement une fois. Un « % » résiduel après ce
        // passage restera un caractère refusé — c'est ce qui neutralise l'encodage doublé.
        var decoded = new StringBuilder(requested.Length);
        for (int index = 0; index < requested.Length; index++)
        {
            char current = requested[index];
            if (current != '%')
            {
                decoded.Append(current);
                continue;
            }

            if (index + 2 >= requested.Length
                || !Uri.IsHexDigit(requested[index + 1])
                || !Uri.IsHexDigit(requested[index + 2]))
            {
                return "refus:caractere";
            }

            decoded.Append((char)Convert.ToInt32(requested.Substring(index + 1, 2), 16));
            index += 2;
        }

        // Étape 2 : un séparateur Windows est un séparateur, pas un caractère anodin.
        string normalized = decoded.ToString().Replace('\\', '/');

        if (normalized.StartsWith('/'))
        {
            return "refus:absolu";
        }

        foreach (char current in normalized)
        {
            bool allowed = current is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')
                or '.' or '_' or '-' or '/';
            if (!allowed)
            {
                return "refus:caractere";
            }
        }

        var segments = new List<string>();
        foreach (string segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                // On ne résout pas une remontée pour voir où elle mène : sa présence suffit.
                return "refus:remontee";
            }

            segments.Add(segment);
        }

        return segments.Count == 0 ? "refus:vide" : string.Join("/", segments);
    }

    public static string AdmitVerdict(string journal, string request, int windowSeconds)
    {
        (string nonce, int timestamp) = ParseEntry(request);
        List<(string Nonce, int Timestamp)> entries = journal.Length == 0
            ? []
            : journal.Split(';').Select(ParseEntry).ToList();

        // L'instant courant vient du journal : la défense ne fait pas confiance à l'horloge du client.
        int now = entries.Count == 0 ? timestamp : entries.Max(entry => entry.Timestamp);

        // L'horodatage se contrôle avant le nonce : un rejeu périmé est d'abord une requête périmée.
        if (Math.Abs(now - timestamp) > windowSeconds)
        {
            return "refus:horodatage";
        }

        bool replayed = entries.Any(entry =>
            entry.Nonce == nonce && now - entry.Timestamp <= windowSeconds);
        return replayed ? "refus:rejeu" : "admis";
    }

    private static (string Nonce, int Timestamp) ParseEntry(string entry)
    {
        string[] parts = entry.Split('@', 2);
        return (parts[0], int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
    }
}
