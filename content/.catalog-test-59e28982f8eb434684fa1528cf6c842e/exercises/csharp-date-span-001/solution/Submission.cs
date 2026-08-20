public static class Submission
{
    public static int InclusiveDays(System.DateOnly start, System.DateOnly end)
    {
        // Intervalle inversé : la convention du contrat est zéro, pas un nombre négatif.
        if (end < start)
        {
            return 0;
        }

        // DayNumber compte les jours depuis une origine fixe : la différence donne l'écart,
        // et le +1 inclut les deux bornes — un même jour compte pour un.
        return end.DayNumber - start.DayNumber + 1;
    }
}
