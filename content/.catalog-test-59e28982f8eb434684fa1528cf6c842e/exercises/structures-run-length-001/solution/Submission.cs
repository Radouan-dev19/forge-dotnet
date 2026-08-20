public static class Submission
{
    public static int RunCount(string text)
    {
        // Zéro caractère, zéro groupe : la garde précède l'initialisation à un.
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Le premier caractère ouvre toujours un groupe.
        int runs = 1;

        for (int i = 1; i < text.Length; i++)
        {
            // Un groupe commence exactement là où le caractère diffère du précédent.
            if (text[i] != text[i - 1])
            {
                runs++;
            }
        }

        return runs;
    }
}
