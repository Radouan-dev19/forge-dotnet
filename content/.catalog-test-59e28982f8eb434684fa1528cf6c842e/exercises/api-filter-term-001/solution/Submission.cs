public static class Submission
{
    public static bool ContainsTerm(string value, string term)
    {
        // Sans valeur ou sans terme, aucune correspondance : un terme vide accepté
        // ferait correspondre toutes les lignes, ce qui n'est pas un filtre.
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        // Ordinal insensible à la casse : même verdict sur toute machine, sans règle
        // culturelle — un filtre d'API n'est pas du texte localisé.
        return value.Contains(term.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
