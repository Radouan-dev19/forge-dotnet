public static class Submission
{
    public static string ArtifactName(string branch, int runNumber)
    {
        // Sans branche ou sans numéro positif, l'artefact n'a pas d'identité : refus.
        if (string.IsNullOrWhiteSpace(branch) || runNumber <= 0)
        {
            throw new System.ArgumentException("Identité de run invalide.");
        }

        // Normalisation : bords, casse invariante, et le séparateur de chemin des
        // branches remplacé — un nom de fichier ne contient pas de barre oblique.
        string safe = branch.Trim().ToLowerInvariant().Replace("/", "-");

        return $"tests-{safe}-{runNumber}";
    }
}
