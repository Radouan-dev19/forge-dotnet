using System;
using System.Collections.Generic;

public static class Submission
{
    // La compensation est une décision métier, pas une symétrie de nommage : chaque geste est
    // catalogué et relu — le correctif compense la notification, le remboursement compense le débit.
    private static readonly Dictionary<string, string> Compensators = new(StringComparer.Ordinal)
    {
        ["create-order"] = "void-order",
        ["reserve-stock"] = "release-stock",
        ["charge-card"] = "refund-card",
        ["book-carrier"] = "cancel-carrier",
        ["notify-customer"] = "send-correction",
    };

    public static string CompensationPlan(string completedSteps)
    {
        if (string.IsNullOrWhiteSpace(completedSteps))
        {
            throw new ArgumentException("Un journal vide n'a rien à compenser.", nameof(completedSteps));
        }

        var executed = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string step in completedSteps.Split(','))
        {
            if (!Compensators.ContainsKey(step))
            {
                throw new ArgumentException("Une étape du journal est hors catalogue.", nameof(completedSteps));
            }

            // Deux débits dans un journal de saga : le refus coûte moins cher qu'un double remboursement.
            if (!seen.Add(step))
            {
                throw new ArgumentException("Une étape du journal est répétée.", nameof(completedSteps));
            }

            executed.Add(step);
        }

        // L'ordre inverse garde à chaque instant un état-préfixe : la dernière étape exécutée se
        // défait la première, et une compensation interrompue reste interprétable.
        var plan = new List<string>(executed.Count);
        for (int index = executed.Count - 1; index >= 0; index--)
        {
            plan.Add(Compensators[executed[index]]);
        }

        return string.Join(',', plan);
    }
}
