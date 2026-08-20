public static class Submission
{
    public static int[] WindowSums(int[] values, int size)
    {
        // Une taille non positive ou plus grande que la source ne définit aucune fenêtre.
        if (size <= 0 || size > values.Length)
        {
            return System.Array.Empty<int>();
        }

        int[] result = new int[values.Length - size + 1];
        int sum = 0;

        for (int i = 0; i < values.Length; i++)
        {
            // La somme glisse : l'entrant s'ajoute...
            sum += values[i];

            // ... le sortant se retire dès que la fenêtre a dépassé sa taille...
            if (i >= size)
            {
                sum -= values[i - size];
            }

            // ... et chaque fenêtre complète se publie à sa position.
            if (i >= size - 1)
            {
                result[i - size + 1] = sum;
            }
        }

        return result;
    }
}
