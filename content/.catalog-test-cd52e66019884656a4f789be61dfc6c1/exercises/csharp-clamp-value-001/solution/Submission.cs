public static class Submission
{
    public static int Clamp(int value, int minimum, int maximum)
    {
        // Un intervalle inversé n'est pas un intervalle : le signaler, pas le réparer.
        if (minimum > maximum)
        {
            throw new System.ArgumentException("Bornes inversées.");
        }

        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        // Dans l'intervalle, bornes comprises : la valeur passe inchangée.
        return value;
    }
}
