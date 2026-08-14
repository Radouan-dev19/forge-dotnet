public static class Submission
{
    public static bool Balanced(string text)
    {
        int depth = 0;

        foreach (char character in text)
        {
            if (character == '(')
            {
                depth++;
            }
            else if (character == ')')
            {
                depth--;

                // Une fermeture sans ouverture rend la profondeur négative : refus immédiat,
                // la suite du texte ne peut pas réparer ce qui est déjà déséquilibré.
                if (depth < 0)
                {
                    return false;
                }
            }
        }

        // Équilibré exige aussi que tout ce qui est ouvert soit refermé à la fin.
        return depth == 0;
    }
}
