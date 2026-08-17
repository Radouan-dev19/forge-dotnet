using System;

public static class Submission
{
    private const int Buckets = 100;

    public static bool ShouldSample(int traceHash, int percent, bool isError)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(percent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percent, Buckets);

        // La seule exception à l'échantillonnage, et elle passe avant tout calcul : une trace en
        // erreur est celle qu'on relira, la perdre revient à ne pas tracer du tout.
        if (isError)
        {
            return true;
        }

        // Le reste d'une division entière prend le signe du dividende : une empreinte négative
        // donnerait un seau négatif, toujours inférieur au taux, donc toujours conservé.
        int bucket = ((traceHash % Buckets) + Buckets) % Buckets;

        // Comparaison stricte : les seaux vont de zéro à quatre-vingt-dix-neuf, un taux de n en
        // retient donc exactement n. La borne large en garderait un de trop.
        return bucket < percent;
    }
}
