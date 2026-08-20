public static class Submission
{
    public static int RecursiveSum(int[] values)
    {
        return Sum(values, 0);

        // Fonction locale : le tableau ne change jamais, seul l'index progresse.
        static int Sum(int[] items, int index) =>
            index == items.Length
                ? 0
                : checked(items[index] + Sum(items, index + 1));
    }
}
