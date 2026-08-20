public static class Submission
{
    public static int[] MergeSorted(int[] left, int[] right)
    {
        int[] result = new int[left.Length + right.Length];
        int i = 0;
        int j = 0;
        int k = 0;

        // Tant qu'une des deux sources n'est pas épuisée, on consomme la plus petite tête.
        while (i < left.Length || j < right.Length)
        {
            // On prend à gauche si la droite est épuisée, ou si la tête gauche est
            // inférieure ou égale : le « ou égal » garde les doublons dans l'ordre gauche.
            bool takeLeft = j >= right.Length || (i < left.Length && left[i] <= right[j]);

            if (takeLeft)
            {
                result[k] = left[i];
                i++;
            }
            else
            {
                result[k] = right[j];
                j++;
            }

            k++;
        }

        return result;
    }
}
