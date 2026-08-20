using System;

public static class Submission
{
    public static int DrainMinutes(int backlog, int arrivalsPerMinute, int consumedPerMinute)
    {
        // Un débit négatif ne mesure rien ; une consommation nulle, elle, est un état réel — un
        // consommateur arrêté — et sa réponse est simplement l'impossibilité.
        ArgumentOutOfRangeException.ThrowIfNegative(backlog);
        ArgumentOutOfRangeException.ThrowIfNegative(arrivalsPerMinute);
        ArgumentOutOfRangeException.ThrowIfNegative(consumedPerMinute);

        // La fonction répond à la question posée : cet arriéré-ci. Le débit net défavorable d'une
        // file déjà vide est un autre signal, qui mérite sa propre alerte.
        if (backlog == 0)
        {
            return 0;
        }

        // Seul le débit net résorbe : les producteurs continuent pendant le drainage.
        if (consumedPerMinute <= arrivalsPerMinute)
        {
            return -1;
        }

        int net = consumedPerMinute - arrivalsPerMinute;

        // Quotient plafond en entier large : la somme intermédiaire déborde le trente-deux bits
        // quand l'arriéré est immense et le net minuscule — le cas des grandes pannes.
        return (int)(((long)backlog + net - 1) / net);
    }
}
