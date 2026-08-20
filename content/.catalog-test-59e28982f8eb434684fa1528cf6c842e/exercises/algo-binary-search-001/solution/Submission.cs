public static class Submission
{
    public static int BinarySearch(int[] values, int target)
    {
        int left = 0;
        int right = values.Length - 1;

        // Intervalle fermé [left, right] : la boucle vit tant qu'il reste au moins un candidat.
        while (left <= right)
        {
            // Écrit ainsi, le milieu ne déborde jamais, même si left et right sont énormes.
            int middle = left + (right - left) / 2;

            if (values[middle] == target)
            {
                return middle;
            }

            if (values[middle] < target)
            {
                // La cible est strictement à droite : le milieu testé sort de l'intervalle.
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        // Intervalle vidé sans rencontre : la valeur est absente du tableau.
        return -1;
    }
}
