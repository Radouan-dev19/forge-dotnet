using System;

public static class Submission
{
    public static string BulkheadVerdict(int capacity, int inFlight, int queueCapacity, int queued)
    {
        // Une capacité d'exécution nulle est un service condamné par configuration ; une file
        // nulle, elle, est le cloisonnement le plus strict et reste légitime.
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(queueCapacity);

        // Une occupation au-delà de sa capacité ne décrit aucun instant réel : c'est un compteur
        // qui a fui, et le refus fait remonter ce défaut-là plutôt que de décider dessus.
        if (inFlight < 0 || inFlight > capacity || queued < 0 || queued > queueCapacity)
        {
            throw new ArgumentException("Le relevé du cloisonnement est incohérent.", nameof(inFlight));
        }

        // L'exécution se vérifie avant la file : attendre quand un emplacement est libre ferait
        // payer une attente pour rien.
        if (inFlight < capacity)
        {
            return "execute|slot-available";
        }

        if (queued < queueCapacity)
        {
            return "enqueue|slots-full";
        }

        // Le rejet rapide rend la main : c'est le signal qui empêche la panne de voyager.
        return "reject|bulkhead-saturated";
    }
}
