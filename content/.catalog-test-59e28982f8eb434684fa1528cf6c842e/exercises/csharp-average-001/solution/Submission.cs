public static class Submission
{
    public static decimal AverageOrZero(int[] values)
    {
        // La convention du contrat : sans données, la moyenne vaut zéro plutôt que de lever.
        if (values is null || values.Length == 0)
        {
            return 0m;
        }

        // Somme en 64 bits : le cumul de beaucoup d'int ne doit pas déborder un int.
        long sum = 0;

        foreach (int value in values)
        {
            sum += value;
        }

        // La conversion en decimal se fait AVANT la division, sinon elle serait entière.
        return (decimal)sum / values.Length;
    }
}
