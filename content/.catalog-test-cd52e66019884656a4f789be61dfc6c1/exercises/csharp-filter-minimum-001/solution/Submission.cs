using System.Linq;

public static class Submission
{
    public static int[] AtLeast(int[] values, int minimum)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Where conserve l'ordre de la source ; la borne est incluse par le >=.
        return values
            .Where(value => value >= minimum)
            .ToArray();
    }
}
