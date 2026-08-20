public static class Submission
{
    public static decimal LineTotal(decimal unitPrice, int quantity)
    {
        // Les deux invariants du domaine se vérifient avant tout calcul.
        if (unitPrice < 0m || quantity < 0)
        {
            throw new System.ArgumentOutOfRangeException();
        }

        // Multiplier d'abord, arrondir une seule fois à la fin : l'arrondi intermédiaire
        // fabriquerait des écarts d'un centime qui s'accumulent.
        return decimal.Round(unitPrice * quantity, 2, System.MidpointRounding.AwayFromZero);
    }
}
