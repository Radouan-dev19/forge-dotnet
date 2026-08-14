public static class Submission
{
    public static System.Collections.Generic.Dictionary<string, int> Frequencies(int[] values)
    {
        var result = new System.Collections.Generic.Dictionary<string, int>(
            System.StringComparer.Ordinal);

        foreach (int value in values)
        {
            // La clé textuelle se forme en culture invariante : le signe moins reste le
            // même caractère sur toutes les machines.
            string key = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Une seule entrée par clé : lire le compte courant, écrire le compte plus un.
            result[key] = result.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        return result;
    }
}
