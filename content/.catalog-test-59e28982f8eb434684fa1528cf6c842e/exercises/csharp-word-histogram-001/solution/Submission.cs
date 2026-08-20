public static class Submission
{
    public static System.Collections.Generic.Dictionary<string, int> WordHistogram(string text)
    {
        // Le comparateur du dictionnaire porte l'insensibilité à la casse du contrat :
        // Chat et chat cumulent dans la même entrée, sans normaliser les clés.
        var result = new System.Collections.Generic.Dictionary<string, int>(
            System.StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        // RemoveEmptyEntries absorbe les espaces répétés et les espaces de bord.
        foreach (string word in text.Split(" ", System.StringSplitOptions.RemoveEmptyEntries))
        {
            // Lire le compte courant (zéro par défaut), écrire le compte incrémenté.
            result[word] = result.TryGetValue(word, out int count) ? count + 1 : 1;
        }

        return result;
    }
}
