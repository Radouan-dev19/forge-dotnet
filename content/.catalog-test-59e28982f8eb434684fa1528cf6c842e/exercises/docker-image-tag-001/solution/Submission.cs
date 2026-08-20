using System;

public static class Submission
{
    private const string MovingTag = "latest";

    public static string ResolveTag(string reference, bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(reference);

        string trimmed = reference.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("La référence d'image est obligatoire.", nameof(reference));
        }

        // Le dernier deux-points, jamais le premier : un registre privé s'écrit avec un port.
        int separator = trimmed.LastIndexOf(':');

        // Sans deux-points, la référence porte implicitement l'étiquette mouvante. C'est le cas
        // qu'une implémentation naïve laisse passer, et c'est le plus fréquent en production.
        string tag = separator < 0 ? MovingTag : trimmed[(separator + 1)..];

        if (isProduction && string.Equals(tag, MovingTag, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Une étiquette mouvante ne désigne pas une image reproductible.", nameof(reference));
        }

        return trimmed;
    }
}
