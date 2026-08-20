using System;
using System.Collections.Generic;

public static class Submission
{
    public static string ConsumerAction(int deliveryCount, string messageId, string processedIds)
    {
        // Bornes d'abord : un message livre moins d'une fois est une entree absurde.
        if (deliveryCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryCount));
        }

        // Mise a l'ecart avant toute autre chose : un message trop livre est empoisonne
        // et ne doit plus etre retente, meme s'il figure parmi les deja traites.
        if (deliveryCount > 5)
        {
            return "dead-letter";
        }

        // Ensemble des identifiants deja traites, segments vides ecartes, comparaison exacte.
        var processed = new HashSet<string>(
            processedIds.Split(',', StringSplitOptions.RemoveEmptyEntries),
            StringComparer.Ordinal);

        // Doublon : le message a deja produit son effet, on l'acquitte sans le rejouer.
        if (processed.Contains(messageId))
        {
            return "ack-duplicate";
        }

        // Cas nominal : message valide, pas trop livre, jamais traite.
        return "process";
    }
}
