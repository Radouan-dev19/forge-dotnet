public static class Submission
{
    public static int MinimumIndex(int[] values)
    {
        // Le contrat définit le tableau absent ou vide : indice impossible, donc moins un.
        if (values is null || values.Length == 0)
        {
            return -1;
        }

        // L'accumulateur est un indice, pas une valeur : c'est lui que la fonction promet.
        int min = 0;

        for (int i = 1; i < values.Length; i++)
        {
            // Strictement inférieur : à égalité, le premier minimum rencontré reste gagnant.
            if (values[i] < values[min])
            {
                min = i;
            }
        }

        return min;
    }
}
