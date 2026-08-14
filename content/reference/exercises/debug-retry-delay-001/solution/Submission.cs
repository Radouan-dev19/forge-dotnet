public static class Submission
{
    public static int RetryDelay(int attempt)
    {
        // Un numéro de tentative négatif ne décrit rien : faute d'appel.
        if (attempt < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(attempt));
        }

        // L'exposant est plafonné AVANT le décalage : c'est le plafond qui rend la
        // croissance exponentielle finie, et le décalage borné qui reste défini.
        int power = System.Math.Min(attempt, 5);

        // 100 ms doublées à chaque tentative : 100, 200, 400... plafonné à 3200.
        return 100 * (1 << power);
    }
}
