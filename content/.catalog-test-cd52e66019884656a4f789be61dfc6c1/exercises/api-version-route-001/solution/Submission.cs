using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ResolveApiVersion(string requestPath, string supportedVersions, string defaultVersion)
    {
        var options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        string[] segments = (requestPath ?? "").Split('/', options);

        // Pas de premier segment : aucune version demandée, on sert le défaut.
        if (segments.Length == 0)
        {
            return defaultVersion;
        }

        string first = segments[0].ToLowerInvariant();

        // Le premier segment n'a pas la forme d'une version : c'est une ressource,
        // donc aucune version demandée.
        if (!LooksLikeVersion(first))
        {
            return defaultVersion;
        }

        var supported = new HashSet<string>(
            (supportedVersions ?? "").Split(',', options),
            StringComparer.OrdinalIgnoreCase);

        // Version demandée : servie si connue, refusée sinon — jamais rabattue en silence.
        return supported.Contains(first) ? first : "unsupported";
    }

    private static bool LooksLikeVersion(string segment)
    {
        if (segment.Length < 2 || segment[0] != 'v')
        {
            return false;
        }

        for (int index = 1; index < segment.Length; index++)
        {
            if (!char.IsDigit(segment[index]))
            {
                return false;
            }
        }

        return true;
    }
}
