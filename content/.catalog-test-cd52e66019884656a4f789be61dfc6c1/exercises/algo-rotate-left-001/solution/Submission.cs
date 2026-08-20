public static class Submission
{
    public static int[] RotateLeft(int[] values, int offset)
    {
        // Sans cette garde, le modulo par la longueur diviserait par zéro.
        if (values.Length == 0)
        {
            return System.Array.Empty<int>();
        }

        // Double modulo : ramène d'abord offset dans ]-n, n[, puis dans [0, n[.
        // C'est ce qui rend les décalages négatifs et les tours complets corrects.
        int shift = ((offset % values.Length) + values.Length) % values.Length;

        int[] result = new int[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            // La case i du résultat vient de la case i + shift de l'entrée, modulo n.
            result[i] = values[(i + shift) % values.Length];
        }

        return result;
    }
}
