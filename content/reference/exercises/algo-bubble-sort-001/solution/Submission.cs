public static class Submission
{
    public static int[] BubbleSort(int[] values)
    {
        // Le contrat interdit de modifier l'entrée : tout le travail se fait sur une copie.
        int[] result = (int[])values.Clone();

        // Après chaque passage, le plus grand élément restant est à sa place définitive :
        // la zone à trier rétrécit par la droite, d'où la borne mobile end.
        for (int end = result.Length - 1; end > 0; end--)
        {
            for (int i = 0; i < end; i++)
            {
                if (result[i] > result[i + 1])
                {
                    // Échange par déconstruction de tuples : pas de variable temporaire.
                    (result[i], result[i + 1]) = (result[i + 1], result[i]);
                }
            }
        }

        return result;
    }
}
