public static class Submission
{
    public static decimal Median(int[] values)
    {
        // Convention du contrat : sans données, la médiane vaut zéro.
        if (values is null || values.Length == 0)
        {
            return 0m;
        }

        // Le tri est nécessaire mais il mute : il s'applique à une copie, jamais aux
        // données observées.
        int[] copy = (int[])values.Clone();
        System.Array.Sort(copy);

        int middle = copy.Length / 2;

        // Longueur impaire : l'élément central. Paire : la moyenne des deux centraux,
        // calculée en decimal pour ne pas perdre la demi-unité.
        return copy.Length % 2 == 1
            ? copy[middle]
            : ((decimal)copy[middle - 1] + copy[middle]) / 2m;
    }
}
