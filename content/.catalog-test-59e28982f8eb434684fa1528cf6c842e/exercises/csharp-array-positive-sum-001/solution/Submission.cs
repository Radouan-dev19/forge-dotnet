public static class Submission
{
    public static int PositiveSum(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        int sum = 0;

        foreach (int value in values)
        {
            // Strictement positif : zéro n'apporte rien et les négatifs sont exclus du contrat.
            if (value > 0)
            {
                // Le cumul est vérifié : un débordement lève au lieu de s'enrouler.
                sum = checked(sum + value);
            }
        }

        return sum;
    }
}
