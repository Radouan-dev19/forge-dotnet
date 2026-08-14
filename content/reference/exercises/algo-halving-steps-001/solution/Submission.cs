public static class Submission
{
    public static int HalvingSteps(int value)
    {
        // Le contrat ne définit rien pour un négatif : refuser vaut mieux que d'inventer.
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(value));
        }

        int steps = 0;

        // On compte les divisions réellement effectuées : zéro et un n'en demandent aucune.
        while (value > 1)
        {
            value /= 2;
            steps++;
        }

        return steps;
    }
}
