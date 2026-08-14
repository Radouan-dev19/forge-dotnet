using System;

public static class Submission
{
    public static int TokensAfterRequest(int tokensBefore, int capacity, int refilled)
    {
        // Une capacité nulle n'a pas de sens ; une recharge négative décrirait une fuite.
        if (capacity <= 0 || refilled < 0 || tokensBefore < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        // Addition en 64 bits : une longue inactivité pourrait déborder un int avant plafond.
        long refilledTotal = (long)tokensBefore + refilled;

        // Le plafond s'applique À LA RECHARGE : le seau se remplit jusqu'au bord, pas au-delà.
        int available = (int)Math.Min(capacity, refilledTotal);

        // Un jeton disponible : l'appel passe et le consomme. Sinon, refus sans consommation.
        return available > 0 ? available - 1 : available;
    }
}
