public static class Submission
{
    public static int DistinctEventCount(string events)
    {
        if (string.IsNullOrWhiteSpace(events))
        {
            return 0;
        }

        // Découper, ignorer les segments vides, dédupliquer en ordinal : la casse
        // distingue — A et a sont deux événements — et rien d'autre n'est normalisé.
        return new System.Collections.Generic.HashSet<string>(
            events.Split(",", System.StringSplitOptions.RemoveEmptyEntries),
            System.StringComparer.Ordinal).Count;
    }
}
