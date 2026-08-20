using System;
using System.Globalization;

public static class Submission
{
    private const int SegmentCount = 3;

    public static int CompareVersions(string left, string right)
    {
        int[] leftParts = ReadVersion(left, nameof(left));
        int[] rightParts = ReadVersion(right, nameof(right));

        for (int rank = 0; rank < SegmentCount; rank++)
        {
            // Arrêt au premier écart : poursuivre laisserait un segment mineur renverser une
            // décision que le majeur avait déjà tranchée.
            if (leftParts[rank] != rightParts[rank])
            {
                return leftParts[rank] > rightParts[rank] ? 1 : -1;
            }
        }

        return 0;
    }

    private static int[] ReadVersion(string version, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(version, parameterName);

        string[] segments = version.Split('.');
        if (segments.Length != SegmentCount)
        {
            throw new ArgumentException("Une version porte exactement trois segments.", parameterName);
        }

        var parsed = new int[SegmentCount];
        for (int rank = 0; rank < SegmentCount; rank++)
        {
            // La conversion en nombre est la règle : comparée comme du texte, la dixième
            // correction passerait avant la neuvième.
            if (!int.TryParse(segments[rank], NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                throw new ArgumentException("Chaque segment est un entier positif ou nul.", parameterName);
            }

            parsed[rank] = value;
        }

        return parsed;
    }
}
