public static class Submission
{
    public static int MaximumDepth(string text)
    {
        int depth = 0;
        int maximum = 0;

        foreach (char c in text)
        {
            if (c == '(')
            {
                depth++;

                // Le pic se relève à chaque ouverture : c'est le seul moment où il monte.
                maximum = System.Math.Max(maximum, depth);
            }
            else if (c == ')')
            {
                depth--;

                // Fermeture orpheline : structure déséquilibrée, verdict moins un immédiat.
                if (depth < 0)
                {
                    return -1;
                }
            }
        }

        // Des ouvertures restées sans fermeture invalident aussi la mesure.
        return depth == 0 ? maximum : -1;
    }
}
