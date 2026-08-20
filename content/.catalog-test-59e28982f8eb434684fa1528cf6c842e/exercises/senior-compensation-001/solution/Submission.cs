using System;
using System.Collections.Generic;

public static class Submission
{
    public static string CompensationOrder(string completedSteps)
    {
        ArgumentNullException.ThrowIfNull(completedSteps);

        // Les etapes reussies, dans l'ordre ou elles ont ete appliquees.
        var applied = new List<string>();
        foreach (string rawStep in completedSteps.Split(';'))
        {
            string step = rawStep.Trim();
            if (step.Length > 0)
            {
                applied.Add(step);
            }
        }

        // On defait dans l'ordre inverse : la derniere etape posee est la premiere annulee.
        var compensations = new List<string>();
        for (int index = applied.Count - 1; index >= 0; index--)
        {
            compensations.Add("undo-" + applied[index]);
        }

        return string.Join(";", compensations);
    }
}
