public static class Submission
{
    public static int Factorial(int value)
    {
        // Le domaine est doublement borné : pas de négatif, et pas au-delà de douze,
        // dernière factorielle qui tienne dans un int.
        if (value < 0 || value > 12)
        {
            throw new System.ArgumentOutOfRangeException(nameof(value));
        }

        // Cas de base : zéro et un valent un. Sinon, l'appel réduit strictement la valeur.
        return value <= 1
            ? 1
            : checked(value * Factorial(value - 1));
    }
}
