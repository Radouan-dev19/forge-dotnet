public static class Submission
{
    public static int[] SelectionSort(int[] values)
    {
        // Copie défensive : le contrat promet une entrée intacte, le harnais le vérifie.
        int[] result = (int[])values.Clone();

        // Invariant : result[0..start-1] contient les plus petites valeurs, triées et
        // définitives. Chaque tour y ajoute le minimum de la zone restante.
        for (int start = 0; start < result.Length; start++)
        {
            int min = start;

            for (int i = start + 1; i < result.Length; i++)
            {
                if (result[i] < result[min])
                {
                    min = i;
                }
            }

            // Un seul échange par tour, même si min == start : l'échange avec soi-même
            // est sans effet et évite une branche.
            (result[start], result[min]) = (result[min], result[start]);
        }

        return result;
    }
}
