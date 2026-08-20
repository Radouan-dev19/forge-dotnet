public static class Submission
{
    public static int GreatestCommonDivisor(int left, int right)
    {
        // Le PGCD est défini sur les valeurs absolues : on normalise les signes d'entrée
        // une fois pour toutes, plutôt que d'y penser à chaque tour de boucle.
        left = System.Math.Abs(left);
        right = System.Math.Abs(right);

        // Algorithme d'Euclide : remplacer le couple (a, b) par (b, a mod b) conserve
        // exactement l'ensemble des diviseurs communs, et b décroît strictement.
        while (right != 0)
        {
            int remainder = left % right;
            left = right;
            right = remainder;
        }

        // Quand le reste atteint zéro, le dernier diviseur non nul est le PGCD.
        return left;
    }
}
