using System.Linq;

public static class Submission
{
    public static int[] TopThree(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Ordonner en descendant puis borner : Take(3) rend moins d'éléments si la
        // source en a moins, et les doublons comptent chacun pour un.
        return values
            .OrderByDescending(value => value)
            .Take(3)
            .ToArray();
    }
}
