public static class Submission
{
    public static string NextPatch(string version)
    {
        string[] parts = version?.Split('.') ?? []; if (parts.Length != 3 || !int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor) || !int.TryParse(parts[2], out int patch) || major < 0 || minor < 0 || patch < 0) throw new System.ArgumentException("Version invalide."); return $"{major}.{minor}.{checked(patch + 1)}";
    }
}
