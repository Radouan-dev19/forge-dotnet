public static class Submission
{
    public static bool HasBearerToken(string header)
    {
        // Absent ou blanc : rien à vérifier, verdict négatif sans exception.
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        // Le schéma se compare sans casse, comme la norme des en-têtes le prévoit.
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Le schéma seul ne prouve rien : il faut une preuve non blanche à sa suite —
        // et le verdict ne transporte jamais la valeur de cette preuve.
        return header.Length > prefix.Length
            && !string.IsNullOrWhiteSpace(header.Substring(prefix.Length));
    }
}
