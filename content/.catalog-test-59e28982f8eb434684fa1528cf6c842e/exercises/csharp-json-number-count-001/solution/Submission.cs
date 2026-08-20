public static class Submission
{
    public static int JsonNumberCount(string json)
    {
        // Une entrée absente ou blanche ne porte aucun document : zéro, par convention.
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        // Parse valide la syntaxe entière ; le using libère le document après lecture.
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);

        // Le contrat ne compte que dans un tableau racine : tout autre document rend zéro.
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return 0;
        }

        int count = 0;

        foreach (System.Text.Json.JsonElement item in document.RootElement.EnumerateArray())
        {
            // ValueKind distingue le nombre JSON de la chaîne qui contient des chiffres.
            if (item.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                count++;
            }
        }

        return count;
    }
}
