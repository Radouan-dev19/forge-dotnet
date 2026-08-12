using System;

public static class Submission
{
    // Vrai dès qu'au moins une règle du catalogue s'applique au panier.
    public static bool IsEligible(decimal total, int itemCount, bool isMember)
    {
        throw new NotImplementedException();
    }

    // Montant de la règle la plus avantageuse, arrondi à deux décimales. Zéro si aucune.
    public static decimal BestDiscount(decimal total, int itemCount, bool isMember)
    {
        throw new NotImplementedException();
    }

    // « <cle> -> <montant> », l'égalité étant tranchée par l'ordre de déclaration.
    public static string ExplainDecision(decimal total, int itemCount, bool isMember)
    {
        throw new NotImplementedException();
    }
}
