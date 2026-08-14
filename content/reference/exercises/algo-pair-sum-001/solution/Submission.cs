public static class Submission
{
    public static bool HasPairSum(int[] values, int target)
    {
        var seen = new System.Collections.Generic.HashSet<int>();

        foreach (int value in values)
        {
            // Chercher le complément AVANT d'ajouter la valeur courante : une paire
            // exige deux positions distinctes, jamais deux fois la même case.
            if (seen.Contains(target - value))
            {
                return true;
            }

            seen.Add(value);
        }

        return false;
    }
}
