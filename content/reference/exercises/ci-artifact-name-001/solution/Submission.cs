public static class Submission
{
    public static string ArtifactName(string branch, int runNumber)
    {
        if (string.IsNullOrWhiteSpace(branch) || runNumber <= 0) throw new System.ArgumentException("Identité de run invalide."); string safe = branch.Trim().ToLowerInvariant().Replace("/", "-"); return $"tests-{safe}-{runNumber}";
    }
}
