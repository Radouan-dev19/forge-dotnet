using System;

public static class Submission
{
    public static int FirstRegression(string reads)
    {
        if (string.IsNullOrWhiteSpace(reads))
        {
            throw new ArgumentException("Un journal de lectures vide ne se vérifie pas.", nameof(reads));
        }

        string[] entries = reads.Split(';');
        int previous = -1;

        for (int index = 0; index < entries.Length; index++)
        {
            if (!int.TryParse(entries[index], out int version) || version < 0)
            {
                throw new ArgumentException("Une version lue est illisible ou négative.", nameof(reads));
            }

            // Le recul est strict : l'égalité dit que la donnée n'a pas bougé, pas que le client
            // a changé de réplique. L'indice fautif est celui de la lecture servie en retard.
            if (index > 0 && version < previous)
            {
                return index;
            }

            previous = version;
        }

        return -1;
    }
}
