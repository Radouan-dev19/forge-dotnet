public static class Submission
{
    public static string NextPatch(string version)
    {
        // L'absence se fond dans le cas général : zéro segment, donc invalide.
        string[] parts = version?.Split('.') ?? [];

        // Trois segments, tous entiers non négatifs : tout écart est une version
        // illisible, refusée d'un bloc plutôt qu'interprétée.
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int major)
            || !int.TryParse(parts[1], out int minor)
            || !int.TryParse(parts[2], out int patch)
            || major < 0 || minor < 0 || patch < 0)
        {
            throw new System.ArgumentException("Version invalide.");
        }

        // Seul le composant de correction bouge ; le checked couvre la borne extrême.
        return $"{major}.{minor}.{checked(patch + 1)}";
    }
}
