public static class Submission
{
    public static System.Collections.Generic.List<int> CopyValues(
        System.Collections.Generic.List<int> values)
    {
        if (values is null)
        {
            throw new System.ArgumentNullException(nameof(values));
        }

        // Le constructeur de copie matérialise une liste indépendante, dans l'ordre,
        // doublons compris : modifier l'une ne touche plus l'autre.
        return new System.Collections.Generic.List<int>(values);
    }
}
