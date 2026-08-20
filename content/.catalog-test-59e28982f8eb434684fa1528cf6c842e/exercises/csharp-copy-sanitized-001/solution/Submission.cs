public static class Submission
{
    public static int[] SanitizeCopy(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Le nettoyage s'écrit dans une collection neuve : l'appelant garde son original.
        int[] copy = new int[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            // Plancher à zéro : les négatifs sont neutralisés, le reste passe tel quel.
            copy[i] = System.Math.Max(0, values[i]);
        }

        return copy;
    }
}
