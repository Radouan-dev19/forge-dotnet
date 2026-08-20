using System;
using System.Collections.Generic;

public static class Submission
{
    // Nombre d'entrées portant ce niveau. La casse du niveau demandé est sans importance.
    public static int CountBySeverity(string logs, string severity)
    {
        throw new NotImplementedException();
    }

    // Regroupe les seules entrées ERROR par message normalisé : toute suite de chiffres devient #.
    public static Dictionary<string, int> GroupByMessage(string logs)
    {
        throw new NotImplementedException();
    }

    // « <n> x <message> » par ligne, la plus fréquente en tête, l'égalité tranchée par l'ordre ordinal.
    public static string ErrorReport(string logs)
    {
        throw new NotImplementedException();
    }
}
