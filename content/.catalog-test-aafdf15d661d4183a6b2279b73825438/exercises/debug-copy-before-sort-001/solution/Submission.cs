public static class Submission
{
    public static int[] SortedCopy(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Array.Sort trie EN PLACE : sans cette copie préalable, c'est le tableau de
        // l'appelant — celui que le débogueur observe — qui serait réordonné.
        int[] copy = (int[])values.Clone();
        System.Array.Sort(copy);
        return copy;
    }
}
