public static class Submission
{
    public static int MaximumOrZero(int[] values)
    {
        // Le nom l'annonce : sans données, la valeur de repli est zéro.
        if (values is null || values.Length == 0)
        {
            return 0;
        }

        // L'accumulateur démarre sur une donnée réelle, jamais sur une constante :
        // un tableau entièrement négatif rend ainsi son vrai maximum.
        int max = values[0];

        foreach (int value in values)
        {
            if (value > max)
            {
                max = value;
            }
        }

        return max;
    }
}
