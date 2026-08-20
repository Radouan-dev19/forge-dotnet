public static class Submission
{
    public static bool IsLocalRedirect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Un chemin local commence par un séparateur unique. Les formes // et /\
        // sont des adresses réseau relatives au protocole : elles sortent du site.
        return value.StartsWith("/", System.StringComparison.Ordinal)
            && !value.StartsWith("//", System.StringComparison.Ordinal)
            && !value.StartsWith("/\\", System.StringComparison.Ordinal);
    }
}
