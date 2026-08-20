public static class Submission
{
    public static bool HasConflictMarkers(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // Les trois marqueurs se cherchent séparément : un conflit à moitié résolu
        // laisse parfois un seul des trois, et il suffit à casser la fusion.
        return text.Contains("<<<<<<<", System.StringComparison.Ordinal)
            || text.Contains("=======", System.StringComparison.Ordinal)
            || text.Contains(">>>>>>>", System.StringComparison.Ordinal);
    }
}
