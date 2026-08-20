using System;

public static class Submission
{
    // Au-delà de cet écart, le contexte autour des lignes reportées a probablement changé.
    private const int DriftThreshold = 50;

    public static string CherryPickRisk(int commitsBetween, bool touchesSameFiles, bool isMergeCommit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(commitsBetween);

        // Une fusion a deux parents : sans dire par rapport auquel les modifications se mesurent,
        // le report n'a pas de sens unique.
        if (isMergeCommit)
        {
            return "refuse";
        }

        // L'écart mesure une dérive de contexte ; une dérive dans des fichiers que le commit ne
        // touche pas ne le concerne pas. Deux cents commits sans fichier commun sont plus sûrs
        // que trois commits sur le même fichier.
        if (!touchesSameFiles)
        {
            return "low";
        }

        return commitsBetween > DriftThreshold ? "high" : "medium";
    }
}
