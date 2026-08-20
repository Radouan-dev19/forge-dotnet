public static class Submission
{
    public static string FirstUnique(string text)
    {
        var counts = new System.Collections.Generic.Dictionary<char, int>();

        // Première passe : compter chaque caractère.
        foreach (char c in text)
        {
            counts[c] = counts.TryGetValue(c, out int count) ? count + 1 : 1;
        }

        // Seconde passe, dans l'ordre d'origine : le premier compte à un gagne.
        foreach (char c in text)
        {
            if (counts[c] == 1)
            {
                return c.ToString();
            }
        }

        // Aucun caractère unique : chaîne vide, par convention du contrat.
        return "";
    }
}
