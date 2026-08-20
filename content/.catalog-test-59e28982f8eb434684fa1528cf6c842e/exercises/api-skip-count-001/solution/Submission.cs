public static class Submission
{
    public static int SkipCount(int page, int pageSize)
    {
        // Les pages se comptent depuis un, et la taille respecte le plafond public.
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new System.ArgumentOutOfRangeException();
        }

        // La page un ne saute rien : d'où le moins un. Le produit est vérifié pour
        // qu'une page immense lève au lieu de déborder en silence.
        return checked((page - 1) * pageSize);
    }
}
