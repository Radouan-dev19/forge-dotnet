public static class Submission
{
    public static bool IsBoundary(int value, int minimum, int maximum)
    {
        // Des bornes inversées ne décrivent aucun intervalle : refus nommé.
        if (minimum > maximum)
        {
            throw new System.ArgumentException("Bornes inversées.");
        }

        // Une frontière est l'une des deux extrémités exactes, rien d'autre.
        return value == minimum || value == maximum;
    }
}
