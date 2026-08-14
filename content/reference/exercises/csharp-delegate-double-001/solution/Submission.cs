public static class Submission
{
    public static int[] DoubleAll(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Le comportement est injecté sous forme de lambda : ConvertAll l'applique à chaque
        // case et écrit dans un tableau neuf — la source reste intacte.
        return System.Array.ConvertAll(values, value => checked(value * 2));
    }
}
