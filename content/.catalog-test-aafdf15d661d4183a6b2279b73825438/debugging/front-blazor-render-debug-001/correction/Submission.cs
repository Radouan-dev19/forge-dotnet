using System;

public static class Submission
{
    // Reconstitue l'etiquette affichee par un composant a partir des mises a jour d'un parametre.
    public static string DisplayedValue(string parameterUpdates)
    {
        string[] updates = Split(parameterUpdates);
        if (updates.Length == 0)
        {
            return "";
        }

        string current = updates[updates.Length - 1];
        return "total:" + current;
    }

    private static string[] Split(string parameterUpdates) =>
        parameterUpdates.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
