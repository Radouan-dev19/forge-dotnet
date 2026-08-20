using System;

public static class Submission
{
    public static string ConsumerAction(int deliveryCount, string messageId, string processedIds)
    {
        // Appliquez les regles dans l'ordre : bornes du compteur, mise a l'ecart,
        // doublon deja traite, puis traitement nominal.
        throw new NotImplementedException("La decision du consommateur reste a ecrire.");
    }
}
