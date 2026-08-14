public static class Submission
{
    public static int Maximum(int[] values)
    {
        // Convention du contrat : l'absence de données rend zéro plutôt qu'une exception.
        if (values is null || values.Length == 0)
        {
            return 0;
        }

        // Partir du premier élément, jamais de zéro : un tableau tout négatif doit
        // rendre son plus grand élément, pas la valeur d'initialisation.
        int max = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > max)
            {
                max = values[i];
            }
        }

        return max;
    }
}
