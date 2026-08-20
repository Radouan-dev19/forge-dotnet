using System.Linq;

public static class Submission
{
    public static int[] DistinctSorted(int[] values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Dédupliquer d'abord, trier ensuite : le tri travaille alors sur moins d'éléments.
        // ToArray matérialise une seule fois, en fin de chaîne.
        return values
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }
}
