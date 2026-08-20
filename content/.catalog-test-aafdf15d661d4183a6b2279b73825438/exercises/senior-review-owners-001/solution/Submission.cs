using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string RequiredReviewers(string owners, string files)
    {
        if (string.IsNullOrWhiteSpace(owners) || string.IsNullOrWhiteSpace(files))
        {
            throw new ArgumentException("La carte des propriétés et les fichiers sont requis.", nameof(owners));
        }

        var ownership = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in owners.Split(';'))
        {
            string[] parts = entry.Split('=');
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0
                || !ownership.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Une propriété de la carte est illisible ou répétée.", nameof(owners));
            }
        }

        var summoned = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in files.Split(','))
        {
            if (file.Length == 0)
            {
                throw new ArgumentException("Un chemin de fichier est vide.", nameof(files));
            }

            // Le plus long préfixe couvrant l'emporte : la propriété exacte d'un fichier bat
            // toujours celle de ses répertoires, l'englobant restant le repli.
            string? bestPrefix = null;
            foreach (KeyValuePair<string, string> property in ownership)
            {
                bool covers = property.Key.EndsWith('/')
                    ? file.StartsWith(property.Key, StringComparison.Ordinal)
                    : file == property.Key;
                if (covers && (bestPrefix is null || property.Key.Length > bestPrefix.Length))
                {
                    bestPrefix = property.Key;
                }
            }

            // La zone grise est le trou du mécanisme : du code sans propriétaire est du code sans
            // relecteur, et la carte se met à jour maintenant, pas à l'incident.
            if (bestPrefix is null)
            {
                throw new ArgumentException("Un fichier n'a aucun propriétaire déclaré.", nameof(files));
            }

            summoned.Add(ownership[bestPrefix]);
        }

        return string.Join(',', summoned.OrderBy(owner => owner, StringComparer.Ordinal));
    }
}
