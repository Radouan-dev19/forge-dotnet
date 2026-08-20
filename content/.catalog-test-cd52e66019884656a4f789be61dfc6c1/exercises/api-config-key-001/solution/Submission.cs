public static class Submission
{
    public static string ConfigKey(string section, string key)
    {
        // Une clé incomplète au démarrage vaut mieux qu'une valeur introuvable en pleine
        // charge : le refus est immédiat et nommé.
        if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
        {
            throw new System.ArgumentException("Clé incomplète.");
        }

        // Le deux-points est le séparateur hiérarchique standard de la configuration.
        return $"{section.Trim()}:{key.Trim()}";
    }
}
