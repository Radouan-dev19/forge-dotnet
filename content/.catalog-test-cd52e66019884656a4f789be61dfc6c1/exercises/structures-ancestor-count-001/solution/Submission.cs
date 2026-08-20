public static class Submission
{
    public static int AncestorCount(int[] parents, int node)
    {
        // Un nœud hors du tableau ne désigne rien : la convention du contrat est moins un.
        if (node < 0 || node >= parents.Length)
        {
            return -1;
        }

        int count = 0;
        int current = node;

        // Remonter de parent en parent jusqu'à la racine, marquée par moins un.
        while (parents[current] != -1)
        {
            current = parents[current];

            // Un parent hors bornes est une donnée corrompue ; un compte qui dépasse la
            // taille du tableau prouve un cycle, car une chaîne sans cycle est plus courte.
            if (current < 0 || current >= parents.Length || ++count > parents.Length)
            {
                return -1;
            }
        }

        return count;
    }
}
