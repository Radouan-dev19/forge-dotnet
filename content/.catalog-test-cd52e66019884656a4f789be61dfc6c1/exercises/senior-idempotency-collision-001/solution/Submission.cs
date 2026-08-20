using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string KeyCollisions(string requests)
    {
        if (string.IsNullOrWhiteSpace(requests))
        {
            throw new ArgumentException("Un journal vide ne s'audite pas.", nameof(requests));
        }

        // L'empreinte de référence est la première vue : c'est elle que le serveur mémorise avec
        // la réponse, donc elle que le mécanisme réel compare.
        var referenceHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var collided = new HashSet<string>(StringComparer.Ordinal);

        foreach (string request in requests.Split(';'))
        {
            string[] parts = request.Split(':');
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                throw new ArgumentException("Une requête du journal est illisible.", nameof(requests));
            }

            if (!referenceHashes.TryAdd(parts[0], parts[1]) && referenceHashes[parts[0]] != parts[1])
            {
                // Empreinte différente sous la même clé : la collision, celle que personne ne voit
                // parce que les deux clients reçoivent une réponse polie.
                collided.Add(parts[0]);
            }
        }

        return string.Join(',', collided.OrderBy(key => key, StringComparer.Ordinal));
    }
}
