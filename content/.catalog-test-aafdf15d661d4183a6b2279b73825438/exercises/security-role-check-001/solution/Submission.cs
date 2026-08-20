public static class Submission
{
    public static bool HasRole(string roles, string required)
    {
        // Sans liste ou sans rôle demandé, rien n'est démontrable : refus calme.
        if (string.IsNullOrWhiteSpace(roles) || string.IsNullOrWhiteSpace(required))
        {
            return false;
        }

        var options = System.StringSplitOptions.RemoveEmptyEntries
            | System.StringSplitOptions.TrimEntries;

        foreach (string role in roles.Split(',', options))
        {
            // Comparaison de segments COMPLETS : jamais de recherche partielle, qui
            // ferait d'Operator un sous-mot suffisant de SuperOperator.
            if (string.Equals(role, required.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
