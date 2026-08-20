public static class Submission
{
    public static int UniqueCount(int[] values)
    {
        // L'absence de tableau est une faute d'appel : elle se signale, elle ne se compte pas.
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // L'ensemble absorbe les doublons à l'insertion : son cardinal est la réponse.
        return new System.Collections.Generic.HashSet<int>(values).Count;
    }
}
