public static class Submission
{
    public static int CountEven(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        int count = 0;

        foreach (int value in values)
        {
            // Le reste modulo deux vaut zéro pour tous les pairs, zéro et négatifs compris :
            // en C#, -4 % 2 vaut 0, donc aucune valeur absolue n'est nécessaire.
            if (value % 2 == 0)
            {
                count++;
            }
        }

        return count;
    }
}
