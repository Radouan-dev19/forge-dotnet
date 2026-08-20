using System;
using System.Collections.Generic;

public static class Submission
{
    public static int[] CoverageBlockers(int[] covered, int[] total, int floorPercent)
    {
        if (covered.Length != total.Length)
        {
            throw new ArgumentException("Chaque module doit déclarer ses deux comptes.", nameof(total));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(floorPercent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(floorPercent, 100);

        var blockers = new List<int>();
        for (int index = 0; index < covered.Length; index++)
        {
            bool corrupt = covered[index] < 0 || total[index] < 0 || covered[index] > total[index];
            if (corrupt)
            {
                throw new ArgumentException("Une mesure de couverture est incohérente.", nameof(covered));
            }

            // Un module sans branche n'a rien à couvrir : le juger reviendrait à diviser par zéro.
            if (total[index] == 0)
            {
                continue;
            }

            // Produits croisés en long : aucun pourcentage flottant, donc aucun arrondi qui offrirait
            // le dixième de point manquant, et aucun débordement sur un dépôt à un milliard de branches.
            if (covered[index] * 100L < floorPercent * (long)total[index])
            {
                blockers.Add(index);
            }
        }

        return blockers.ToArray();
    }
}
