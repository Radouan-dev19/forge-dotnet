using System;
using System.Linq;

public static class Submission
{
    public static int SurvivingMutants(int[] probes, int low, int high)
    {
        if (low > high)
        {
            throw new ArgumentException("Un intervalle vide ne définit aucune règle à muter.", nameof(low));
        }

        int survivors = 0;

        // Durcir une borne ne change la réponse que sur la borne elle-même : le mutant meurt
        // si cette valeur est sondée. Les deux bornes se traitent indépendamment, même confondues.
        if (!probes.Contains(low))
        {
            survivors++;
        }

        if (!probes.Contains(high))
        {
            survivors++;
        }

        // Élargir une borne posée sur une limite du type ne produit aucune règle différente : ce
        // mutant équivalent est ignoré. Le voisin est comparé en long pour ne jamais déborder.
        if (low != int.MinValue && !probes.Any(probe => probe == (long)low - 1))
        {
            survivors++;
        }

        if (high != int.MaxValue && !probes.Any(probe => probe == (long)high + 1))
        {
            survivors++;
        }

        return survivors;
    }
}
