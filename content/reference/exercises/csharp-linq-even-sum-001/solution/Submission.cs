using System.Linq;

public static class Submission
{
    public static int EvenSum(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Une seule chaîne, une seule énumération : le filtre alimente la somme au fil
        // de l'eau, sans collection intermédiaire.
        return values
            .Where(value => value % 2 == 0)
            .Sum();
    }
}
