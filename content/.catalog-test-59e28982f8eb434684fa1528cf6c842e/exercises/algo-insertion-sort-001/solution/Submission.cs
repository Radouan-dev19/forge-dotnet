public static class Submission
{
    public static int[] InsertionSort(int[] values)
    {
        // L'entrée reste intacte : le tri travaille sur sa propre copie.
        int[] result = (int[])values.Clone();

        // Invariant : avant chaque tour, result[0..i-1] est trié. On y insère result[i].
        for (int i = 1; i < result.Length; i++)
        {
            int current = result[i];
            int j = i - 1;

            // Décaler vers la droite tous les éléments du préfixe strictement plus grands
            // que la valeur à insérer : le trou se déplace jusqu'à sa place.
            while (j >= 0 && result[j] > current)
            {
                result[j + 1] = result[j];
                j--;
            }

            result[j + 1] = current;
        }

        return result;
    }
}
