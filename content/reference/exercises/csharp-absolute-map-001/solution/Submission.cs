public static class Submission
{
    public static int[] AbsoluteAll(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // ConvertAll alloue le tableau de sortie et applique la projection case par case :
        // la source n'est jamais écrite, ce que le contrat exige.
        return System.Array.ConvertAll(values, value => System.Math.Abs(value));
    }
}
