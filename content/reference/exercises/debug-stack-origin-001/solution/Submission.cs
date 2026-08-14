public static class Submission
{
    public static string FirstFrame(string trace)
    {
        if (string.IsNullOrWhiteSpace(trace))
        {
            return "";
        }

        foreach (string line in trace.Split('\n'))
        {
            // Chaque ligne se nettoie avant le test : l'indentation des traces varie.
            string trimmed = line.Trim();

            // La première frame du code applicatif — préfixe du produit — est l'origine
            // la plus probable ; les frames système au-dessus ne sont que le trajet.
            if (trimmed.StartsWith("at Forge.", System.StringComparison.Ordinal))
            {
                return trimmed;
            }
        }

        return "";
    }
}
