using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ReplayVerdict(string ledger, string delivery)
    {
        // Le registre vide est l'état de tout consommateur neuf : tout y est première livraison.
        var records = new Dictionary<string, (string Hash, string Status)>(StringComparer.Ordinal);
        if (ledger.Length > 0)
        {
            foreach (string entry in ledger.Split(';'))
            {
                string[] parts = entry.Split(':');
                bool readable = parts.Length == 3 && parts[0].Length > 0 && parts[1].Length > 0
                    && parts[2] is "done" or "failed";
                if (!readable)
                {
                    throw new ArgumentException("Une entrée du registre est illisible.", nameof(ledger));
                }

                // Deux entrées pour le même identifiant : la source de vérité s'est dédoublée.
                if (!records.TryAdd(parts[0], (parts[1], parts[2])))
                {
                    throw new ArgumentException("Le registre se contredit sur un identifiant.", nameof(ledger));
                }
            }
        }

        string[] message = delivery.Split(':');
        if (message.Length != 2 || message[0].Length == 0 || message[1].Length == 0)
        {
            throw new ArgumentException("La livraison est illisible.", nameof(delivery));
        }

        if (!records.TryGetValue(message[0], out (string Hash, string Status) record))
        {
            return "process|first-delivery";
        }

        // La charge se vérifie avant le statut : retenter un contenu divergent appliquerait
        // l'opération recyclée, la corruption même que le registre empêche.
        if (record.Hash != message[1])
        {
            return "reject|payload-mismatch";
        }

        return record.Status == "failed" ? "retry|previous-failure" : "skip|already-applied";
    }
}
