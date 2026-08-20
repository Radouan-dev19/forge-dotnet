public static class Submission
{
    public static int EffectiveTimeout(int requestedSeconds, int maximumSeconds)
    {
        // Une durée nulle ou négative ne décrit aucun budget : refus avant tout calcul.
        if (requestedSeconds <= 0 || maximumSeconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException();
        }

        // Le budget effectif est le plus contraignant des deux : la demande du client
        // ne dépasse jamais le plafond du serveur.
        return System.Math.Min(requestedSeconds, maximumSeconds);
    }
}
