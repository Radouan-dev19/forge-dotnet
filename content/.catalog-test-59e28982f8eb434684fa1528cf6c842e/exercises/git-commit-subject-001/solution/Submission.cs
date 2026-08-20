public static class Submission
{
    public static bool IsCommitSubjectValid(string subject)
    {
        // Un sujet absent ou blanc n'annonce rien : refusé.
        if (string.IsNullOrWhiteSpace(subject))
        {
            return false;
        }

        string value = subject.Trim();

        // Soixante-douze caractères : la largeur au-delà de laquelle les listes de
        // commits tronquent. Le point final est proscrit par la convention des sujets.
        return value.Length <= 72
            && !value.EndsWith(".", System.StringComparison.Ordinal);
    }
}
