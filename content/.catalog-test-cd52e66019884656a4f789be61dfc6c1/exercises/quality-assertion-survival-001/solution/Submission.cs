using System;
using System.Collections.Generic;

public static class Submission
{
    // Natures qui observent le contrat : accessibles à un appelant ordinaire, elles survivent.
    private static readonly HashSet<string> ObservableContract = new(StringComparer.Ordinal)
    {
        "result",
        "error-contract",
        "state",
    };

    // Natures qui épient la mécanique : le remaniement a le droit de changer ce qu'elles mesurent.
    private static readonly HashSet<string> InternalMechanics = new(StringComparer.Ordinal)
    {
        "calls",
        "order",
        "private-state",
        "timing",
    };

    public static string SurvivingAssertions(string assertions)
    {
        if (string.IsNullOrWhiteSpace(assertions))
        {
            throw new ArgumentException("Un inventaire vide ne prépare aucun remaniement.", nameof(assertions));
        }

        var survivors = new List<string>();
        foreach (string kind in assertions.Split(';'))
        {
            if (ObservableContract.Contains(kind))
            {
                survivors.Add(kind);
            }
            else if (!InternalMechanics.Contains(kind))
            {
                // Ignorer un intrus rendrait l'inventaire incomplet au moment où l'équipe s'y fie.
                throw new ArgumentException("Une nature d'assertion du flux est inconnue.", nameof(assertions));
            }
        }

        return string.Join(';', survivors);
    }
}
