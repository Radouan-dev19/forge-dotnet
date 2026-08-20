public static class Submission
{
    public static string NormalizeSort(string value)
    {
        // Absence absorbée puis normalisation d'identifiant : bords, casse invariante.
        string sort = value?.Trim().ToLowerInvariant() ?? "";

        // Liste fermée des clés publiques ; tout le reste retombe sur le tri par défaut.
        return sort is "date" or "total" or "status" ? sort : "id";
    }
}
