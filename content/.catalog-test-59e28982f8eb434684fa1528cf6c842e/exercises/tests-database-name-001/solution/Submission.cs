public static class Submission
{
    public static bool IsIsolatedDatabase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Préfixe réservé en comparaison ordinale, et longueur minimale qui garantit
        // un suffixe d'unicité : les deux conditions font l'isolation.
        return name.StartsWith("forge-test-", System.StringComparison.Ordinal)
            && name.Length >= 20;
    }
}
