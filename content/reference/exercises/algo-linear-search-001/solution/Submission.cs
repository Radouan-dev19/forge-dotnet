public static class Submission
{
    public static int IndexOf(int[] values, int target)
    {
        // Un tableau absent est une erreur d'appel, pas une absence de résultat.
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == target)
            {
                // Première occurrence : on s'arrête dès la rencontre, sans finir le parcours.
                return i;
            }
        }

        // Parcours complet sans rencontre : convention moins un, comme IndexOf de la BCL.
        return -1;
    }
}
