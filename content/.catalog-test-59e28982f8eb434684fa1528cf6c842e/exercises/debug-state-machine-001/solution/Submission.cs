public static class Submission
{
    public static int FinalState(int[] events)
    {
        // État initial : zéro. Les transitions autorisées sont énumérées ; tout le
        // reste est ignoré — un événement hors état ne casse pas la machine.
        int state = 0;

        foreach (int value in events)
        {
            if (value == 1 && state == 0)
            {
                // Démarrage : uniquement depuis l'état initial.
                state = 1;
            }
            else if (value == 2 && state == 1)
            {
                // Achèvement : uniquement depuis l'état démarré.
                state = 2;
            }
            else if (value == 3)
            {
                // Réinitialisation : depuis n'importe quel état.
                state = 0;
            }
        }

        return state;
    }
}
