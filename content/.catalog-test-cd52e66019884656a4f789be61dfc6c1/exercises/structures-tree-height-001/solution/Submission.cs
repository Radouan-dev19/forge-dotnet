public static class Submission
{
    public static int TreeHeight(int[] parents)
    {
        int height = 0;

        // La hauteur de l'arbre est la plus profonde des remontées : une par nœud.
        for (int node = 0; node < parents.Length; node++)
        {
            int depth = 1;
            int current = node;
            int guard = 0;

            while (parents[current] != -1)
            {
                current = parents[current];
                depth++;

                // Plus de pas que de nœuds : un cycle est prouvé, l'arbre est invalide.
                if (++guard > parents.Length)
                {
                    return -1;
                }
            }

            if (depth > height)
            {
                height = depth;
            }
        }

        return height;
    }
}
