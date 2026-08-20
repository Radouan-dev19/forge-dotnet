using System;
using System.Collections.Generic;
using System.Globalization;

public static class Submission
{
    public static bool IsEligible(decimal total, int itemCount, bool isMember)
    {
        return Applicable(total, itemCount, isMember).Count > 0;
    }

    public static decimal BestDiscount(decimal total, int itemCount, bool isMember)
    {
        return Select(total, itemCount, isMember).Amount;
    }

    public static string ExplainDecision(decimal total, int itemCount, bool isMember)
    {
        (string key, decimal amount) = Select(total, itemCount, isMember);
        return key + " -> " + amount.ToString("F2", CultureInfo.InvariantCulture);
    }

    // Le catalogue vit ici, dans son ordre de départage. Ajouter une règle se fait sur cette seule
    // liste : ni la sélection ni l'explication n'ont à la connaître.
    private static List<Rule> Applicable(decimal total, int itemCount, bool isMember)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        ArgumentOutOfRangeException.ThrowIfNegative(itemCount);

        var applicable = new List<Rule>();
        if (itemCount >= 10)
        {
            applicable.Add(new Rule("volume", Round(total * 0.15m)));
        }

        if (total >= 100m)
        {
            applicable.Add(new Rule("panier", Round(12.00m)));
        }

        if (isMember)
        {
            applicable.Add(new Rule("adhesion", Round(total * 0.05m)));
        }

        return applicable;
    }

    private static Rule Select(decimal total, int itemCount, bool isMember)
    {
        var best = new Rule("aucune", 0.00m);
        bool chosen = false;
        foreach (Rule rule in Applicable(total, itemCount, isMember))
        {
            // Comparaison stricte : à montant égal, la règle déjà retenue — donc la plus ancienne
            // dans le catalogue — conserve la main.
            if (!chosen || rule.Amount > best.Amount)
            {
                best = rule;
                chosen = true;
            }
        }

        return best;
    }

    private static decimal Round(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private readonly record struct Rule(string Key, decimal Amount);
}
